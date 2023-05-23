namespace ResXManager.Translators
{
    using Azure;
    using Azure.AI.OpenAI;
    using global::Microsoft.DeepDev;
    using Newtonsoft.Json;
    using ResXManager.Infrastructure;
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using TomsToolbox.Essentials;
    using JsonConvert = Newtonsoft.Json.JsonConvert;
    using PromptList = System.Collections.Generic.List<(Infrastructure.ITranslationItem item, string prompt)>;

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

            // determine if we are using a chat model
            if (ModelName.StartsWith("gpt-", true, CultureInfo.InvariantCulture))
            {
                await TranslateUsingChatModel(translationSession, client).ConfigureAwait(false);
            }
            else
            {
                await TranslateUsingCompletionsModel(translationSession, client).ConfigureAwait(false);
            }
        }

        private async Task TranslateUsingChatModel(ITranslationSession translationSession, OpenAIClient client)
        {
            var retries = 0;

            var itemsByLanguage = translationSession.Items.GroupBy(item => item.TargetCulture);

            foreach (var languageGroup in itemsByLanguage)
            {
                var cultureKey = languageGroup.Key;
                var targetCulture = cultureKey.Culture ?? translationSession.NeutralResourcesLanguage;

                foreach (var batch in PackMessagesIntoBatches(translationSession, languageGroup.ToList(), targetCulture))
                {
                retry:
                    try
                    {
                        // call Azure OpenAI API with all prompts in batch
                        var options = new ChatCompletionsOptions()
                        {
                            Temperature = Temperature,
                            MaxTokens = CompletionTokens,
                            Messages = { batch.message }
                        };
                        var completionsResponse = await client.GetChatCompletionsAsync(
                            deploymentOrModelName: ModelDeploymentName,
                            options, translationSession.CancellationToken
                        ).ConfigureAwait(false);

                        if (completionsResponse.HasValue)
                        {
                            await translationSession.MainThread.StartNew(() => ReturnResults(batch.items, completionsResponse.Value)).ConfigureAwait(false);
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
        }

        private IEnumerable<(ChatMessage message, List<ITranslationItem> items)> PackMessagesIntoBatches(ITranslationSession translationSession,
            List<ITranslationItem> items, CultureInfo targetCulture)
        {
            var batchItems = new List<ITranslationItem>();
            var batchTokens = 0;
            var tokenizer = TokenizerBuilder.CreateByModelName(ModelName ?? throw new InvalidOperationException("No model name provided in configuration!"));
            ChatMessage? message = null;

            foreach (var item in items)
            {
                batchItems.Add(item);

                message = GenerateMessageForTranslations(translationSession, batchItems, targetCulture);
                if (message is null)
                {
                    translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                var tokens = tokenizer.Encode(message.Content, new List<string>()).Count;

                if (tokens > PromptTokens)
                {
                    translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                if ((batchTokens + tokens) > PromptTokens)
                {
                    yield return (message, batchItems);
                    batchItems = new List<ITranslationItem>();
                    batchTokens = 0;
                    message = null;
                }
                else
                {
                    batchTokens += tokens;
                }
            }

            if (message is not null)
            {
                yield return (message, batchItems);
            }
        }

        private ChatMessage? GenerateMessageForTranslations(ITranslationSession translationSession,
            IEnumerable<ITranslationItem> items, CultureInfo targetCulture)
        {
            if (!items.Any())
            {
                return null;
            }

            var contentBuilder = new StringBuilder();
#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            contentBuilder.Append($"You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language \"{targetCulture.Name}\", adapting the style and tone to different types of texts.\n");
#pragma warning restore CA1305 // Specify IFormatProvider

            // optionally add custom prompt
            if (!CustomPrompt.IsNullOrWhiteSpace())
            {
                contentBuilder.Append(CustomPrompt);
                contentBuilder.Append('\n');
            }

            // generate a list of items to be translated
            var sources = items.Select(i => i.AllSources
                .Select(s => {
                    var source = new Dictionary<string, string?>()
                    {
                        { (s.Key.Culture ?? translationSession.NeutralResourcesLanguage).Name, s.Text },
                    };
                    if (IncludeCommentsInPrompt &&
                        i.AllComments.SingleOrDefault(c => c.Key == i.TargetCulture) is { Text: string } comment)
                    {
                        source.Add("Context", comment.Text);
                    }
                    return source;
                })
            ).ToList();

            if (sources.Count > 1)
            {
#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple integer
                contentBuilder.Append($"Each item in the following JSON array contains a list of words or sentences with the same meaning in different languages. Translate the {sources.Count} items in the array into the target language \"{targetCulture.Name}\".\n\n");
#pragma warning restore CA1305 // Specify IFormatProvider

                // serialize into JSON
                contentBuilder.Append(JsonConvert.SerializeObject(sources, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
                contentBuilder.Append("\n\n");

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple integer
                contentBuilder.Append($"Respond only with a flat JSON string array of translations to the target language \"{targetCulture.Name}\" of the {sources.Count} items from the source array. Keep the same order of the items as in the source array. Do not include the target language property, only respond with a flat string array with {sources.Count} items.\n\n");
#pragma warning restore CA1305 // Specify IFormatProvider
            }
            else
            {
#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple integer
                contentBuilder.Append($"The following JSON object describes a list of words or sentences with the same meaning in different languages. Translate into the target language \"{targetCulture.Name}\".\n\n");
#pragma warning restore CA1305 // Specify IFormatProvider

                // serialize into JSON
                contentBuilder.Append(JsonConvert.SerializeObject(sources.Single(), Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
                contentBuilder.Append("\n\n");

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple integer
                contentBuilder.Append($"Respond only with flat JSON string array containing the single translation to the target language \"{targetCulture.Name}\" of the source object. Do not include the target language property, only respond with a flat string array containing a single item.\n\n");
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            return new ChatMessage(ChatRole.User, contentBuilder.ToString());
        }

        private void ReturnResults(List<ITranslationItem> batchItems, ChatCompletions completions)
        {
            if (!completions.Choices.Any())
            {
                throw new InvalidOperationException("Azure OpenAI API returned no results.");
            }

            if (completions.Choices[0] is { FinishReason: null or "stop" } choice &&
                choice.Message is { Role.Label: "assistant" } message)
            {
                // deserialize the message content from JSON
                var results = JsonConvert.DeserializeObject<List<string>>(message.Content)
                    ?? throw new InvalidOperationException("Azure OpenAI API returned an empty result.");

                if (batchItems.Count != results.Count)
                {
                    throw new InvalidOperationException("Azure OpenAI API returned a different number of results than requested.");
                }

                batchItems.ForEach(i => i.Results.Add(new TranslationMatch(this, results[batchItems.IndexOf(i)], Ranking)));
            }
            else
            {
                // todo: log the unsuccessful finish reason somewhere for the user to see why this translation failed?
                // expected reasons to get here are "content_filter" or empty response
            }
        }

        private async Task TranslateUsingCompletionsModel(ITranslationSession translationSession, OpenAIClient client)
        {
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

        private IEnumerable<PromptList> PackPromptsIntoBatches(ITranslationSession translationSession)
        {
            var batch = new PromptList();
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
                    batch = new PromptList();
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

            // target language for translation
            var targetCulture = (translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage).Name;

            var promptBuilder = new StringBuilder();
#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            promptBuilder.Append($"You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language \"{targetCulture}\", adapting the style and tone to different types of texts.\n");
#pragma warning restore CA1305 // Specify IFormatProvider

            // optionally add custom prompt
            if (!CustomPrompt.IsNullOrWhiteSpace())
            {
                promptBuilder.Append(CustomPrompt);
                promptBuilder.Append('\n');
            }

            // optionally add comments to prompt
            if (IncludeCommentsInPrompt && translationItem.AllComments.Any())
            {
                promptBuilder.Append("CONTEXT:\n");
                translationItem.AllComments
                    .Select(s => $"{(s.Key.Culture ?? translationSession.NeutralResourcesLanguage).Name}: {s.Text}\n")
                    .ForEach(s => promptBuilder.Append(s));
            }

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            promptBuilder.Append($"Here is a list of words or sentences with the same meaning in different languages. Continue the list of translations for the target language \"{targetCulture}\".\n");
#pragma warning restore CA1305 // Specify IFormatProvider
            promptBuilder.Append("TRANSLATIONS:\n");

            // add all existing translations to prompt
            translationItem.AllSources
                .Select(s => $"{(s.Key.Culture ?? translationSession.NeutralResourcesLanguage).Name}: {s.Text}\n")
                .ForEach(s => promptBuilder.Append(s));

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string concatenation
            // the target language is the last language in the prompt, note that the prompt must not end with a space due to tokenization issues
            promptBuilder.Append($"{targetCulture}:");
#pragma warning restore CA1305 // Specify IFormatProvider
            return promptBuilder.ToString();
        }

        private void ReturnResults(PromptList batch, Completions completions)
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