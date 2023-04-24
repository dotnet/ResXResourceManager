namespace ResXManager.Translators
{
    using Azure;
    using Azure.AI.OpenAI;
    using ResXManager.Infrastructure;
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using TomsToolbox.Essentials;

    [Export(typeof(ITranslator)), Shared]
    public class AzureOpenAITranslator : TranslatorBase
    {
        public AzureOpenAITranslator()
            : base("AzureOpenAI", "AzureOpenAI", null, GetCredentials())
        {
        }

        [DataMember]
        public bool IncludeCommentsInPrompt { get; set; } = true;

        [DataMember]
        public int MaxTokens { get; set; } = 1024;

        [DataMember]
        public float Temperature { get; set; } = 0.3f;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            if (AuthenticationKey.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage("Azure OpenAI Translator requires API key.");
                return;
            }

            if (Url.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage("Azure OpenAI Translator requires URL to resource.");
                return;
            };

            if (ModelDeploymentName.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage("Azure OpenAI Translator requires name of the model deployment (text-davinci-003 recommended).");
                return;
            };

            var client = new OpenAIClient(
                new Uri(Url),
                new AzureKeyCredential(AuthenticationKey)
                );

            var retries = 0;

            foreach (var translationItem in translationSession.Items)
            {
retry:
                try
                {
                    await TranslateItem(translationSession, client, translationItem).ConfigureAwait(false);
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

        private async Task TranslateItem(ITranslationSession translationSession, OpenAIClient client, ITranslationItem translationItem)
        {
            if (translationItem != null && translationItem.AllSources.Any())
            {
                var promptBuilder = new StringBuilder();
                promptBuilder.Append("You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You can write well in the target language, adapting the style and tone to different types of texts. Respond with the target language only. Do not add any text before or after the translation.\n");

                // optionally add comments to prompt
                var comments = translationItem.AllComments
                        .Where(s => s.Item1.Culture is not null && !s.Item2.IsNullOrWhiteSpace());
                if (IncludeCommentsInPrompt && comments.Any())
                {
                    promptBuilder.Append("CONTEXT:\n");
                    comments
                        // ! already filtered for null culture
                        .Select(s => $"{s.Item1.Culture!.Name}: {s.Item2}\n")
                        .ForEach(s => promptBuilder.Append(s));
                }

                promptBuilder.Append("TRANSLATIONS:\n");

                translationItem.AllSources
                    .Where(s => s.Item1.Culture is not null && !s.Item2.IsNullOrWhiteSpace())
                    // ! already filtered for null culture
                    .Select(s => $"{s.Item1.Culture!.Name}: {s.Item2}\n")
                    .ForEach(s => promptBuilder.Append(s));

                // add language to translate into
                var targetCulture = (translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage).Name;
#pragma warning disable CA1305 // Specify IFormatProvider
                promptBuilder.Append($"{targetCulture}: ");
#pragma warning restore CA1305 // Specify IFormatProvider

                var completionsResponse = await client.GetCompletionsAsync(
                    deploymentOrModelName: ModelDeploymentName,
                    new CompletionsOptions()
                    {
                        Temperature = Temperature,
                        MaxTokens = MaxTokens,
                        StopSequences = { "\n" },
                        Prompts = { promptBuilder.ToString() }
                    },
                    translationSession.CancellationToken
                ).ConfigureAwait(false);

                if (completionsResponse.HasValue)
                {
                    await translationSession.MainThread.StartNew(() => ReturnResults(translationItem, completionsResponse.Value)).ConfigureAwait(false);
                }
                else
                {
                    throw new ApplicationException("No response from Azure OpenAI API.");
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

        private string? AuthenticationKey => Credentials[0].Value;

        private void ReturnResults(ITranslationItem item, Completions completions)
        {
            if (completions.Choices[0] is { FinishReason: "stop" } choice &&
                !choice.Text.IsNullOrWhiteSpace())
            {
                item.Results.Add(new TranslationMatch(this, choice.Text, Ranking));
            }
        }

        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[]
            {
                new CredentialItem("AuthenticationKey", "Key"),
                new CredentialItem("Url", "Endpoint Url", false),
                new CredentialItem("ModelDeploymentName", "Model Deployment Name", false),
            };
        }
    }
}