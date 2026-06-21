namespace ResXManager.Translators;

using global::Microsoft.ML.Tokenizers;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using ResXManager.Infrastructure;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TomsToolbox.Essentials;
using JsonConvert = Newtonsoft.Json.JsonConvert;

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string/int concatenations
#pragma warning disable IDE0057 // Use range operator (net472 does not support range operator)
#pragma warning disable CA1865 // Use char overload (net472 does not support char overload)

[Export(typeof(ITranslator)), Shared]
public class AzureOpenAITranslator() : TranslatorBase("AzureOpenAI", "AzureOpenAI", new("https://azure.microsoft.com/en-us/products/cognitive-services/openai-service/"), GetCredentials())
{
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

    // model name used for tokenizer creation; use the canonical name (e.g. "gpt-4o")
    [DataMember(Name = "ModelName")]
    public string? ModelName
    {
        get => Credentials[3].Value;
        set => Credentials[3].Value = value;
    }

    [DataMember]
    // Azure OpenAI API version; bump this for newer models that require it
    public string ApiVersion { get; set; } = "2024-12-01-preview";


    [DataMember]
    // embeds comments inside prompt for further AI guidance per language
    public bool IncludeCommentsInPrompt { get; set; } = true;

    [DataMember]
    // default to max tokens for "gpt-3.5-turbo-instruct" model
    public int MaxTokens { get; set; } = 4096;

    // use half of the tokens for prompting
    private int PromptTokens => MaxTokens / 2;
    // .. and the rest for the completion
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

        ChatClient? chatClient;
        try
        {
            chatClient = CreateAzureOpenAIChatClient(new(Url), AuthenticationKey, ModelDeploymentName);
        }
        catch (Exception e) when (e is ArgumentNullException or ArgumentException or UriFormatException)
        {
            translationSession.AddMessage("Azure OpenAI Translator requires valid Azure resource endpoint URL.");
            return;
        }

        await TranslateUsingChatModel(translationSession, chatClient).ConfigureAwait(false);
    }

    private ChatClient CreateAzureOpenAIChatClient(Uri baseUrl, string authenticationKey, string modelDeploymentName)
    {
        // ! Url and ModelDeploymentName are validated non-null before this method is called
        var endpoint = new Uri(baseUrl, $"openai/deployments/{modelDeploymentName}/");
        var options = new OpenAIClientOptions { Endpoint = endpoint };
        // ! AuthenticationKey is validated non-null before this method is called
        options.AddPolicy(new AzureOpenAIPipelinePolicy(authenticationKey, ApiVersion), PipelinePosition.PerCall);
        // ! ModelDeploymentName is validated non-null before this method is called
        return new OpenAIClient(new ApiKeyCredential("placeholder"), options).GetChatClient(modelDeploymentName);
    }

    private sealed class AzureOpenAIPipelinePolicy(string apiKey, string apiVersion) : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            ApplyAzureHeaders(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            ApplyAzureHeaders(message);
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }

        private void ApplyAzureHeaders(PipelineMessage message)
        {
            message.Request.Headers.Remove("Authorization");
            message.Request.Headers.Set("api-key", apiKey);

            var builder = new UriBuilder(message.Request.Uri);
            var query = builder.Query.TrimStart('?');
            builder.Query = string.IsNullOrEmpty(query)
                ? $"api-version={apiVersion}"
                : $"{query}&api-version={apiVersion}";
            message.Request.Uri = builder.Uri;
        }
    }

    private async Task TranslateUsingChatModel(ITranslationSession translationSession, ChatClient chatClient)
    {
        var tokenizer = TryCreateTokenizerForModel(ModelName);

        var itemsByLanguage = translationSession.Items.GroupBy(item => item.TargetCulture);

        foreach (var languageGroup in itemsByLanguage)
        {
            var cultureKey = languageGroup.Key;
            var neutralResourcesLanguage = translationSession.NeutralResourcesLanguage;
            var targetCulture = cultureKey.Culture ?? neutralResourcesLanguage;
            var cancellationToken = translationSession.CancellationToken;

            foreach (var (content, items) in PackChatModelMessagesIntoBatches(translationSession, languageGroup.ToList(), targetCulture, tokenizer))
            {
                var chatOptions = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = CompletionTokens,
                    Temperature = Temperature,
                };

                try
                {
                    var result = await chatClient.CompleteChatAsync(
                        [ChatMessage.CreateUserMessage(content)],
                        chatOptions,
                        cancellationToken).ConfigureAwait(false);

                    await translationSession.MainThread.StartNew(
                        () => ReturnResults(items, result.Value),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (ClientResultException ex) when (ex.Status == 429)
                {
                    translationSession.AddMessage("Azure OpenAI call failed with too many requests. Consider reducing batch size or waiting before retrying.");
                }
                catch (ClientResultException ex)
                {
                    translationSession.AddMessage($"Azure OpenAI call failed: {ex.Message}");
                }
            }
        }
    }

    private IEnumerable<(string content, ICollection<ITranslationItem> items)> PackChatModelMessagesIntoBatches(ITranslationSession translationSession, IEnumerable<ITranslationItem> items, CultureInfo targetCulture, TiktokenTokenizer? tokenizer)
    {
        var batchItems = new List<ITranslationItem>();
        var batchTokens = 0;
        string? batchContent = null;

        foreach (var item in items)
        {
            var currentBatch = batchItems.Concat([item]).ToList();

            var currentContent = GenerateChatModelMessageForTranslations(translationSession, currentBatch, targetCulture);
            if (currentContent is null)
            {
                translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                continue;
            }

            var tokens = tokenizer?.CountTokens(currentContent) ?? 0;
            if (tokens > PromptTokens)
            {
                translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                continue;
            }

            if (!BatchRequests)
            {
                yield return (currentContent, currentBatch);
                continue;
            }

            if (batchContent is not null && (batchTokens + tokens) > PromptTokens)
            {
                yield return (batchContent, batchItems);

                batchItems = [];
                batchTokens = 0;
            }

            batchContent = currentContent;
            batchItems.Add(item);
            batchTokens += tokens;
        }

        if (batchContent is not null && batchItems.Any())
        {
            yield return (batchContent, batchItems);
        }
    }

    private string? GenerateChatModelMessageForTranslations(ITranslationSession translationSession, List<ITranslationItem> items, CultureInfo targetCulture)
    {
        if (items.Count == 0)
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

        return contentBuilder.ToString();
    }

    private void ReturnResults(ICollection<ITranslationItem> batchItems, ChatCompletion completion)
    {
        if (completion.FinishReason == ChatFinishReason.Stop)
        {
            var messageContent = completion.Content.Count > 0
                ? completion.Content[0].Text
                : throw new InvalidOperationException("Azure OpenAI API did not return any content.");

            var results = JsonConvert.DeserializeObject<List<string>>(FixJsonResponse(messageContent)) ?? throw new InvalidOperationException("Azure OpenAI API returned an empty result.");

            if (batchItems.Count != results.Count)
            {
                throw new InvalidOperationException("Azure OpenAI API returned a different number of results than requested.");
            }

            batchItems.ForEach(i => i.Results.Add(new TranslationMatch(this, results[batchItems.IndexOf(i)], Ranking)));
        }
        else
        {
            // expected finish reasons here are "content_filter" or "length"
        }
    }

    // fix broken JSON by adding missing brackets around arrays
    private static string FixJsonResponse(string json)
    {
        // start by trimming whitespace
        var fixedContent = json.Trim();

        if (fixedContent.StartsWith("`", StringComparison.Ordinal) || fixedContent.EndsWith("`", StringComparison.Ordinal))
        {
            // remove Markdown code block
            fixedContent = fixedContent.Trim('`');

            // detect Markdown code block language
            if (fixedContent.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                // remove from start of content
                fixedContent = fixedContent.Substring(4);
            }

            // trim any whitespace again
            fixedContent = fixedContent.Trim();
        }

        if (fixedContent.StartsWith("{", StringComparison.Ordinal) || fixedContent.EndsWith("}", StringComparison.Ordinal))
        {
            // remove leading and trailing brackets
            fixedContent = fixedContent.Trim('{', '}');

            // trim any whitespace again
            fixedContent = fixedContent.Trim();

            // surround with brackets again
            fixedContent = $"{{{fixedContent}}}";
        }

        if (fixedContent.StartsWith("[", StringComparison.Ordinal) || fixedContent.EndsWith("]", StringComparison.Ordinal))
        {
            // remove leading and trailing brackets
            fixedContent = fixedContent.Trim('[', ']');

            // trim any whitespace again
            fixedContent = fixedContent.Trim();

            // surround with brackets again
            fixedContent = $"[{fixedContent}]";
        }

        return fixedContent;
    }

    private string? AuthenticationKey => Credentials[0].Value;

    private static TiktokenTokenizer? TryCreateFallbackTokenizer()
    {
        try
        {
            return TiktokenTokenizer.CreateForEncoding("o200k_base");
        }
        catch
        {
            return null;
        }
    }

    private static TiktokenTokenizer? TryCreateTokenizerForModel(string? modelName)
    {
        if (modelName.IsNullOrWhiteSpace())
        {
            return TryCreateFallbackTokenizer();
        }

        try
        {
            return TiktokenTokenizer.CreateForModel(modelName);
        }
        catch
        {
            return TryCreateFallbackTokenizer();
        }
    }

    private static IList<ICredentialItem> GetCredentials()
    {
        return
        [
            new CredentialItem("AuthenticationKey", "Key"),
            new CredentialItem("Url", "Endpoint Url", false),
            new CredentialItem("ModelDeploymentName", "Model Deployment Name", false),
            new CredentialItem("ModelName", "Model Name", false),
        ];
    }
}
