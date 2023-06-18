namespace ResXManager.Translators
{
    using global::Microsoft.DeepDev;
    using Newtonsoft.Json;
    using ResXManager.Infrastructure;
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using TomsToolbox.Essentials;
    using JsonConvert = Newtonsoft.Json.JsonConvert;
    using PromptList = System.Collections.Generic.List<(Infrastructure.ITranslationItem item, string prompt)>;

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string/int concatenations

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
        // default to max tokens for "text-davinci-003" model
        public int MaxTokens { get; set; } = 4096;

        // use half of the tokens for prompting
        private int PromptTokens => MaxTokens / 2;
        // .. and the rest for for completion
        private int CompletionTokens => MaxTokens - PromptTokens;

        [DataMember]
        // increase this (up to 2.0) to make the AI more creative
        public float Temperature { get; set; }

        [DataMember]
        // additional text to be embedded into the prompt for all translations
        public string CustomPrompt { get; set; } = "";

        [DataMember]
        // toggles batching of requests on/off
        public bool BatchRequests { get; set; } = true;

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
            }

            if (ModelName.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage($"Azure OpenAI Translator requires name of the model used in the deployment.");
                return;
            }

            if (ModelDeploymentName.IsNullOrWhiteSpace())
            {
                translationSession.AddMessage($"Azure OpenAI Translator requires name of the deployment for \"{ModelName}\".");
                return;
            }

            // using HttpClient instead of Azure.AI.OpenAI package since it won't load in VSIX
            // todo: should reuse sockets or use IHttpClientFactory to avoid socket exhaustion
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", AuthenticationKey);

            try
            {
                client.BaseAddress = new Uri(Url, UriKind.Absolute);
            }
            catch (Exception e) when (e is ArgumentNullException || e is ArgumentException || e is UriFormatException)
            {
                translationSession.AddMessage("Azure OpenAI Translator requires valid Azure resource endpoint URL.");
                return;
            }

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

        private sealed class ChatMessage
        {
            [JsonProperty("role")]
            public string? Role { get; set; }

            [JsonProperty("content")]
            public string? Content { get; set; }
        }

        private sealed class ChatCompletionsChoice
        {
            [JsonProperty("message")]
            public ChatMessage? Message { get; set; }

            [JsonProperty("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private sealed class ChatCompletionsResponse
        {
            [JsonProperty("choices")]
            public IList<ChatCompletionsChoice>? Choices { get; set; }
        }

        private async Task TranslateUsingChatModel(ITranslationSession translationSession, HttpClient client)
        {
            const string ApiVersion = "2023-05-15";
            var endpointUri = new Uri($"/openai/deployments/{ModelDeploymentName}/chat/completions?api-version={ApiVersion}", UriKind.Relative);

            var retries = 0;

            var itemsByLanguage = translationSession.Items.GroupBy(item => item.TargetCulture);

            foreach (var languageGroup in itemsByLanguage)
            {
                var cultureKey = languageGroup.Key;
                var neutralResourcesLanguage = translationSession.NeutralResourcesLanguage;
                var targetCulture = cultureKey.Culture ?? neutralResourcesLanguage;
                var cancellationToken = translationSession.CancellationToken;

                foreach (var (message, items) in PackChatModelMessagesIntoBatches(translationSession, languageGroup.ToList(), targetCulture))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                            // call Azure OpenAI API with all prompts in batch
                            var requestBody = new
                            {
                                temperature = Temperature,
                                max_tokens = CompletionTokens,
                                messages = new[] { message }
                            };

                            using var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                            var completionsResponse = await client.PostAsync(endpointUri, content, cancellationToken).ConfigureAwait(false);

                            // note: net472 does not support System.Net.HttpStatusCode.TooManyRequests
                            if (completionsResponse.StatusCode == (System.Net.HttpStatusCode)429)
                            {
                                var backOffSeconds = 1 << retries++;
                                translationSession.AddMessage($"Azure OpenAI call failed with too many requests. Retrying in {backOffSeconds} second(s).");
                                await Task.Delay(backOffSeconds * 1000, cancellationToken).ConfigureAwait(false);

                            // keep retrying
                            continue;
                        }
                        else
                        {
                            completionsResponse.EnsureSuccessStatusCode();

                            var responseContent = await completionsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                            var completions = JsonConvert.DeserializeObject<ChatCompletionsResponse>(responseContent);

                            await translationSession.MainThread.StartNew(() => ReturnResults(items, completions), cancellationToken).ConfigureAwait(false);

                            // break out of the retry loop
                            break;
                        }

                    }
                }
            }
        }

        private IEnumerable<(ChatMessage message, ICollection<ITranslationItem> items)> PackChatModelMessagesIntoBatches(ITranslationSession translationSession, IEnumerable<ITranslationItem> items, CultureInfo targetCulture)
        {
            var batchItems = new List<ITranslationItem>();
            var batchTokens = 0;
            var tokenizer = TokenizerBuilder.CreateByModelName(ModelName ?? throw new InvalidOperationException("No model name provided in configuration!"));
            ChatMessage? batchMessage = null;

            foreach (var item in items)
            {
                var currentBatch = batchItems.Concat(new[] { item }).ToList();

                var currentMessage = GenerateChatModelMessageForTranslations(translationSession, currentBatch, targetCulture);
                if (currentMessage?.Content is null)
                {
                    translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                var tokens = tokenizer.Encode(currentMessage.Content, new List<string>()).Count;
                if (tokens > PromptTokens)
                {
                    translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                if (!BatchRequests)
                {
                    yield return (currentMessage, currentBatch);
                    continue;
                }

                if (batchMessage is not null && (batchTokens + tokens) > PromptTokens)
                {
                    yield return (batchMessage, batchItems);

                    batchItems = new List<ITranslationItem>();
                    batchTokens = 0;
                }

                batchMessage = currentMessage;
                batchItems.Add(item);
                batchTokens += tokens;
            }

            if (batchMessage is not null && batchItems.Any())
            {
                yield return (batchMessage, batchItems);
            }
        }

        private ChatMessage? GenerateChatModelMessageForTranslations(ITranslationSession translationSession, ICollection<ITranslationItem> items, CultureInfo targetCulture)
        {
            if (!items.Any())
                return null;

            var neutralResourcesLanguage = translationSession.NeutralResourcesLanguage;

            var contentBuilder = new StringBuilder();
            contentBuilder.Append($"You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language \"{targetCulture.Name}\", adapting the style and tone to different types of texts.\n");

            // optionally add custom prompt
            if (!CustomPrompt.IsNullOrWhiteSpace())
            {
                contentBuilder.Append(CustomPrompt);
                contentBuilder.Append('\n');
            }

            // generate a list of items to be translated
            var sources = items.Select(translationItem =>
            {
                var source = new Dictionary<string, string?>();
                var allItems = translationItem.GetAllItems(neutralResourcesLanguage);

                allItems.ForEach(item =>
                {
                    var languageName = item.Culture.Name;
                    source.Add(languageName, item.Text);
                    if (IncludeCommentsInPrompt && item.Comment is { } comment)
                    {
                        source.Add($"{languageName}-comment", comment);
                    }
                });

                return source;
            }).ToList();

            if (sources.Count > 1)
            {
                contentBuilder.Append($"Translate the {sources.Count} items described by the following JSON array into the target language \"{targetCulture.Name}\". Follow any guidance in comments if provided.\n\n");

                // serialize into JSON
                contentBuilder.Append(JsonConvert.SerializeObject(sources, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
                contentBuilder.Append("\n\n");

                contentBuilder.Append($"Respond only with a JSON string array of translations to the target language \"{targetCulture.Name}\" of the {sources.Count} source items. Keep the same order of the items as in the source array. Do not include the target language property, only respond with a flat JSON array with {sources.Count} string items.\n\n");
            }
            else
            {
                contentBuilder.Append($"Translate the item described by the following JSON object into the target language \"{targetCulture.Name}\". Follow any guidance in comments if provided.\n\n");

                // serialize into JSON
                contentBuilder.Append(JsonConvert.SerializeObject(sources.Single(), Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
                contentBuilder.Append("\n\n");

                contentBuilder.Append($"Respond only with a JSON string array containing the single translation to the target language \"{targetCulture.Name}\" of the source item. Do not include the target language property, only respond with a flat JSON array containing a single string item.\n\n");
            }

            return new ChatMessage()
            {
                Role = "user",
                Content = contentBuilder.ToString()
            };
        }

        private void ReturnResults(ICollection<ITranslationItem> batchItems, ChatCompletionsResponse completions)
        {
            if (completions.Choices?.Any() != true)
            {
                throw new InvalidOperationException("Azure OpenAI API returned no results.");
            }

            if (completions.Choices[0] is { FinishReason: null or "stop", Message: { Role: "assistant" } message })
            {
                // deserialize the message content from JSON
                var results = JsonConvert.DeserializeObject<List<string>>(
                    FixJsonArray(message.Content ?? throw new InvalidOperationException("Azure OpenAI API did not return any content."))
                    ) ?? throw new InvalidOperationException("Azure OpenAI API returned an empty result.");

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

        // fix broken JSON by adding missing brackets around arrays
        private static string FixJsonArray(string json)
        {
            var sb = new StringBuilder(json);

            if (!json.StartsWith("[", StringComparison.InvariantCulture))
            {
                sb.Insert(0, '[');
            }
            if (!json.EndsWith("]", StringComparison.InvariantCulture))
            {
                sb.Append(']');
            }

            return sb.ToString();
        }

        private sealed class CompletionsChoice
        {
            [JsonProperty("text")]
            public string? Text { get; set; }

            [JsonProperty("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private sealed class CompletionsResponse
        {
            [JsonProperty("choices")]
            public IList<CompletionsChoice>? Choices { get; set; }
        }

        private async Task TranslateUsingCompletionsModel(ITranslationSession translationSession, HttpClient client)
        {
            const string ApiVersion = "2023-05-15";
            var endpointUri = new Uri($"/openai/deployments/{ModelDeploymentName}/completions?api-version={ApiVersion}", UriKind.Relative);

            var retries = 0;

            var cancellationToken = translationSession.CancellationToken;

            foreach (var batch in PackCompletionModelPromptsIntoBatches(translationSession))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // call Azure OpenAI API with all prompts in batch
                    var requestBody = new
                    {
                        prompt = batch.Select(b => b.prompt),
                        max_tokens = CompletionTokens,
                        temperature = Temperature,
                        stop = new[] { "\n" },
                    };

                    using var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    var completionsResponse = await client.PostAsync(endpointUri, content, cancellationToken).ConfigureAwait(false);

                    // note: net472 does not support System.Net.HttpStatusCode.TooManyRequests
                    if (completionsResponse.StatusCode == (System.Net.HttpStatusCode)429)
                    {
                        var backOffSeconds = 1 << retries++;
                        translationSession.AddMessage($"Azure OpenAI call failed with too many requests. Retrying in {backOffSeconds} second(s).");
                        await Task.Delay(backOffSeconds * 1000, cancellationToken).ConfigureAwait(false);

                        // keep retrying
                        continue;
                    }
                    else
                    {
                        completionsResponse.EnsureSuccessStatusCode();

                        var responseContent = await completionsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var completions = JsonConvert.DeserializeObject<CompletionsResponse>(responseContent);

                        await translationSession.MainThread.StartNew(() => ReturnResults(batch, completions), cancellationToken).ConfigureAwait(false);

                        // break out of the retry loop
                        break;
                    }
                }
            }
        }

        private IEnumerable<PromptList> PackCompletionModelPromptsIntoBatches(ITranslationSession translationSession)
        {
            var batchItems = new PromptList();
            var batchTokens = 0;
            var tokenizer = TokenizerBuilder.CreateByModelName(ModelName ?? throw new InvalidOperationException("No model name provided in configuration!"));

            foreach (var item in translationSession.Items)
            {
                var prompt = GenerateCompletionModelPromptForTranslation(translationSession, item);
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

                if (!BatchRequests)
                {
                    yield return new PromptList { (item, prompt) };
                    continue;
                }

                if ((batchTokens + tokens) > PromptTokens)
                {
                    yield return batchItems;

                    batchItems = new PromptList();
                    batchTokens = 0;
                }

                batchItems.Add((item, prompt));
                batchTokens += tokens;
            }

            if (batchItems.Any())
            {
                yield return batchItems;
            }
        }

        private string? GenerateCompletionModelPromptForTranslation(ITranslationSession translationSession, ITranslationItem translationItem)
        {
            var neutralResourcesLanguage = translationSession.NeutralResourcesLanguage;

            var allItems = translationItem.GetAllItems(neutralResourcesLanguage);

            if (!allItems.Any())
                return null;

            // target language for translation
            var targetCulture = (translationItem.TargetCulture.Culture ?? neutralResourcesLanguage).Name;

            var promptBuilder = new StringBuilder();
            promptBuilder.Append($"You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language \"{targetCulture}\", adapting the style and tone to different types of texts.\n");

            // optionally add custom prompt
            if (!CustomPrompt.IsNullOrWhiteSpace())
            {
                promptBuilder.Append(CustomPrompt);
                promptBuilder.Append('\n');
            }

            // optionally add comments to prompt
            var allComments = allItems
                .Select(item => (item.Culture, item.Comment))
                .Where(item => !item.Comment.IsNullOrWhiteSpace())
                .ToArray();

            if (IncludeCommentsInPrompt && allComments.Any())
            {
                promptBuilder.Append("CONTEXT:\n");
                allComments
                    .Select(s => $"{s.Culture.Name}: {s.Comment}\n")
                    .ForEach(s => promptBuilder.Append(s));
            }

            promptBuilder.Append($"Here is a list of words or sentences with the same meaning in different languages. Continue the list of translations for the target language \"{targetCulture}\".\n");
            promptBuilder.Append("TRANSLATIONS:\n");

            // add all existing translations to prompt
            allItems.Select(s => $"{s.Culture.Name}: {s.Text}\n")
                .ForEach(s => promptBuilder.Append(s));

            // the target language is the last language in the prompt, note that the prompt must not end with a space due to tokenization issues
            promptBuilder.Append($"{targetCulture}:");

            return promptBuilder.ToString();
        }

        private void ReturnResults(PromptList batch, CompletionsResponse completions)
        {
            if (completions?.Choices is null)
            {
                throw new InvalidOperationException("Azure OpenAI API returned no completion choices.");
            }

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

        // this translator is currently adapted to work the best with "text-davinci-003" or "gpt-3.5-turbo"
        [DataMember(Name = "ModelName")]
        public string? ModelName
        {
            get => ExpandModelNameAliases(Credentials[3].Value);
            set => Credentials[3].Value = value;
        }

        private string? AuthenticationKey => Credentials[0].Value;

        private static string? ExpandModelNameAliases(string? modelName)
        {
            // expand alternative model names to known model names

            return modelName switch
            {
                "gpt-35-turbo" => "gpt-3.5-turbo",
                _ => modelName,
            };
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