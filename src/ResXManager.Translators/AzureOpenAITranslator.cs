namespace ResXManager.Translators
{
    using Azure;
    using Azure.AI.OpenAI;
    using global::Microsoft.DeepDev;
    using ResXManager.Infrastructure;
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using TomsToolbox.Essentials;

    using BatchList = System.Collections.Generic.List<(Infrastructure.ITranslationItem item, string prompt)>;

    [Export(typeof(ITranslator)), Shared]
    public class AzureOpenAITranslator : TranslatorBase
    {
        public AzureOpenAITranslator()
            : base(
                  "AzureOpenAI", "AzureOpenAI",
                  new Uri("https://azure.microsoft.com/en-us/products/cognitive-services/openai-service/"),
                  GetCredentials()
                  )
        {
        }

        [DataMember]
        // embeds comments inside prompt for further AI guidance per language
        public bool IncludeCommentsInPrompt { get; set; } = true;

        [DataMember]
        // max tokens for "text-davinci-003" model
        public int MaxTokens { get; set; } = 4096;

        // use half of the tokens for prompting
        private int PromptTokens => MaxTokens / 2;
        // .. and the rest for for completion
        private int CompletionTokens => MaxTokens - PromptTokens;

        [DataMember]
        // increase this (up to 2.0) to make the AI more creative
        public float Temperature { get; set; } = 0f;

        [DataMember]
        // additional text to be embedded into the prompt for all translations
        public string CustomPrompt { get; set; } = "";

        protected override async Task Translate(ITranslationSession translationSession)
        {
            if (AuthenticationKey.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage("Azure OpenAI Translator requires API key.");
                return;
            }

            if (Url.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage("Azure OpenAI Translator requires URL to the Azure resource endpoint.");
                return;
            };

            if (ModelName.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage($"Azure OpenAI Translator requires name of the model used in the deployment.");
                return;
            };

            if (ModelDeploymentName.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage($"Azure OpenAI Translator requires name of the deployment for \"{ModelName}\".");
                return;
            };

            var client = new OpenAIClient(
                new Uri(Url),
                new AzureKeyCredential(AuthenticationKey)
                );

            var retries = 0;

            foreach (var batch in PackPromptsIntoBatches(translationSession))
            {
            retry:
                try
                {
                    // call Azure OpenAI API with all prompts in batch
                    var options = new CompletionsOptions()
                    {
                        Temperature = Temperature,
                        MaxTokens = CompletionTokens,
                        StopSequences = { "\n" },
                    };
                    options.Prompts.AddRange(batch.Select(b => b.prompt));
                    var completionsResponse = await client.GetCompletionsAsync(
                        deploymentOrModelName: ModelDeploymentName,
                        options, translationSession.CancellationToken
                    ).ConfigureAwait(false);

                    if (completionsResponse.HasValue)
                    {
                        await translationSession.MainThread.StartNew(() => ReturnResults(batch, completionsResponse.Value)).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new InvalidOperationException("No response from Azure OpenAI API.");
                    }
                }
                catch (RequestFailedException e)
                {
                    if (e.Status == 429)
                    {
                        var backoffSeconds = 1 << retries++;
                        translationSession.AddMessage($"Azure OpenAI call failed with too many requests. Retrying in {backoffSeconds} second(s).");
                        await Task.Delay(backoffSeconds * 1000).ConfigureAwait(false);
                        goto retry;
                    }
                    else
                    {
                        translationSession.AddMessage($"Azure OpenAI call failed with {e.Message}");
                        return;
                    }
                }
            }
        }

        private IEnumerable<BatchList> PackPromptsIntoBatches(ITranslationSession translationSession)
        {
            var batch = new BatchList();
            var batchTokens = 0;
            var tokenizer = TokenizerBuilder.CreateByModelName(ModelName ?? throw new InvalidOperationException("No model name provided in configuration!"));

            foreach (var item in translationSession.Items)
            {
                var prompt = GeneratePromptForTranslation(translationSession, item);
                if (prompt is null)
                {
                    translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                var tokens = tokenizer.Encode(prompt, new List<string>()).Count;

                if (tokens > PromptTokens)
                {
                    translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                if ((batchTokens + tokens) > PromptTokens)
                {
                    yield return batch;
                    batch = new BatchList();
                    batchTokens = 0;
                }

                batch.Add((item, prompt));
                batchTokens += tokens;
            }

            yield return batch;
        }

        private string? GeneratePromptForTranslation(ITranslationSession translationSession, ITranslationItem translationItem)
        {
            if (translationItem is null || !translationItem.AllSources.Any())
            {
                return null;
            }

            var promptBuilder = new StringBuilder();
            promptBuilder.Append("You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language, adapting the style and tone to different types of texts.\n");

            // optionally add custom prompt
            if (!CustomPrompt.IsNullOrWhiteSpace())
            {
                promptBuilder.Append(CustomPrompt);
                promptBuilder.Append('\n');
            }

            // optionally add comments to prompt
            var comments = translationItem.AllComments
                    .Where(s => !s.Item2.IsNullOrWhiteSpace());
            if (IncludeCommentsInPrompt && comments.Any())
            {
                promptBuilder.Append("CONTEXT:\n");
                comments
                    .Select(s => $"{(s.Item1.Culture ?? translationSession.NeutralResourcesLanguage).Name}: {s.Item2}\n")
                    .ForEach(s => promptBuilder.Append(s));
            }

            // target language for translation
            var targetCulture = (translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage).Name;

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            promptBuilder.Append($"Here is a list of words or sentences with the same meaning in different languages. Continue the list of translations for the target language \"{targetCulture}\".\n");
#pragma warning restore CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            promptBuilder.Append("TRANSLATIONS:\n");

            // add all existing translations to prompt
            translationItem.AllSources
                .Where(s => !s.Item2.IsNullOrWhiteSpace())
                .Select(s => $"{(s.Item1.Culture ?? translationSession.NeutralResourcesLanguage).Name}: {s.Item2}\n")
                .ForEach(s => promptBuilder.Append(s));

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            // the target language is the last language in the prompt, note that the prompt must not end with a space due to tokenization issues
            promptBuilder.Append($"{targetCulture}:");
#pragma warning restore CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            return promptBuilder.ToString();
        }

        [DataMember(Name = "AuthenticationKey")]
        public string? SerializedAuthenticationKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [DataMember(Name = "Url")]
        public string? Url
        {
            get => Credentials[1].Value;
            set => Credentials[1].Value = value;
        }

        [DataMember(Name = "ModelDeploymentName")]
        public string? ModelDeploymentName
        {
            get => Credentials[2].Value;
            set => Credentials[2].Value = value;
        }

        // this translator is currently adapted to work the best with the "text-davinci-003" model
        [DataMember(Name = "ModelName")]
        public string? ModelName
        {
            get => Credentials[3].Value;
            set => Credentials[3].Value = value;
        }

        private string? AuthenticationKey => Credentials[0].Value;

        private void ReturnResults(BatchList batch, Completions completions)
        {
            if (batch.Count != completions.Choices.Count)
            {
                throw new InvalidOperationException("Azure OpenAI API returned a different number of results than requested.");
            }

            for (var i = 0; i < batch.Count; i++)
            {
                if (completions.Choices[i] is { FinishReason: null or "stop" } choice &&
                    !choice.Text.IsNullOrWhiteSpace())
                {
                    batch[i].item.Results.Add(new TranslationMatch(this, choice.Text, Ranking));
                }
                else
                {
                    // todo: log the unsuccessful finish reason somewhere for the user to see why this translation failed?
                    // expected reasons to get here are "content_filter" or empty response
                }
            }
        }

        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[]
            {
                new CredentialItem("AuthenticationKey", "Key"),
                new CredentialItem("Url", "Endpoint Url", false),
                new CredentialItem("ModelDeploymentName", "Model Deployment Name", false),
                new CredentialItem("ModelName", "Model Name", false),
            };
        }
    }
}