namespace ResXManager.Translators;

using global::Microsoft.ML.Tokenizers;
using Newtonsoft.Json;
using ResXManager.Infrastructure;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TomsToolbox.Essentials;
using JsonConvert = Newtonsoft.Json.JsonConvert;

#pragma warning disable CA1305 // Specify IFormatProvider not necessary due to simple string/int concatenations
#pragma warning disable IDE0057 // Use range operator (net472 does not support range operator)
#pragma warning disable CA1865 // Use char overload (net472 does not support char overload)

[Export(typeof(ITranslator)), Shared]
public class OpenAITranslator : TranslatorBase
{
    public OpenAITranslator()
        : base(
            "OpenAI", "OpenAI",
            new Uri("https://openai.com/api/"),
            GetCredentials()
        )
    {
    }

    [DataMember]
    // embeds comments inside prompt for further AI guidance per language
    public bool IncludeCommentsInPrompt { get; set; } = true;

    [DataMember]
    // default to max tokens for "gpt-3.5-turbo-instruct" model
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

    private readonly string Url = "https://api.openai.com";

    protected override async Task Translate(ITranslationSession translationSession)
    {
        if (AuthenticationKey.IsNullOrWhiteSpace())
        {
            translationSession.AddMessage("OpenAI Translator requires API key.");
            return;
        }

        if (ModelName.IsNullOrWhiteSpace())
        {
            translationSession.AddMessage($"OpenAI Translator requires name of the model used in the deployment.");
            return;
        }

        // todo: should reuse sockets or use IHttpClientFactory to avoid socket exhaustion
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthenticationKey}");

        try
        {
            client.BaseAddress = new Uri(Url, UriKind.Absolute);
        }
        catch (Exception e) when (e is ArgumentNullException or ArgumentException or UriFormatException)
        {
            translationSession.AddMessage("OpenAI Translator requires valid resource endpoint URL.");
            return;
        }


        await TranslateUsingCompletionsModel(translationSession, client).ConfigureAwait(false);
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

    private sealed class CompletionsMessage
    {
        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("refusal")]
        public string? Refusal { get; set; }
    }

    private sealed class CompletionsChoice
    {
        [JsonProperty("index")]
        public int? Index { get; set; }

        [JsonProperty("message")]
        public CompletionsMessage? Message { get; set; }

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
        var endpointUri = new Uri($"/v1/chat/completions", UriKind.Relative);
        var tokenizer = TiktokenTokenizer.CreateForModel(
            ModelName ?? throw new InvalidOperationException("No model name provided in configuration!")
            );

        var retries = 0;

        var cancellationToken = translationSession.CancellationToken;

        foreach (var batch in PackCompletionModelPrompts(translationSession, tokenizer))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // call OpenAI API with all prompts in batch
                var requestBody = new
                {
                    messages = new List<ChatMessage>() { new()
                    {
                        Role = "user",
                        Content = batch.prompt
                    }},
                    max_tokens = CompletionTokens,
                    temperature = Temperature,
                    model = ModelName
                };

                using var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var completionsResponse = await client.PostAsync(endpointUri, content, cancellationToken).ConfigureAwait(false);

                // note: net472 does not support System.Net.HttpStatusCode.TooManyRequests
                if (completionsResponse.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    var backOffSeconds = 1 << retries++;
                    translationSession.AddMessage($"OpenAI call failed with too many requests. Retrying in {backOffSeconds} second(s).");
                    await Task.Delay(backOffSeconds * 1000, cancellationToken).ConfigureAwait(false);

                    // keep retrying
                    continue;
                }

                completionsResponse.EnsureSuccessStatusCode();

                var responseContent = await completionsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                var completions = JsonConvert.DeserializeObject<CompletionsResponse>(responseContent);
                if (completions != null)
                {
                    await translationSession.MainThread.StartNew(() => ReturnResults(batch, completions), cancellationToken).ConfigureAwait(false);
                }

                // break out of the retry loop
                break;
            }
        }
    }

    private IEnumerable<(ITranslationItem item, string prompt)> PackCompletionModelPrompts(ITranslationSession translationSession, TiktokenTokenizer tokenizer)
    {
        foreach (var item in translationSession.Items)
        {
            var prompt = GenerateCompletionModelPromptForTranslation(translationSession, item);
            if (prompt is null)
            {
                translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                continue;
            }

            var tokens = tokenizer.CountTokens(prompt);

            if (tokens > PromptTokens)
            {
                translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                continue;
            }

            yield return (item, prompt);
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

    private void ReturnResults((ITranslationItem item, string prompt) batch, CompletionsResponse completions)
    {
        if (completions?.Choices is null)
        {
            throw new InvalidOperationException("OpenAI API returned no completion choices.");
        }

        if (completions.Choices.Count == 0)
        {
            throw new InvalidOperationException("OpenAI API returned a different number of results than requested.");
        }


        if (completions.Choices[0] is { FinishReason: null or "stop" } choice && choice.Message != null &&
            !choice.Message.Content.IsNullOrWhiteSpace())
        {
            batch.item.Results.Add(new TranslationMatch(this, choice.Message.Content, Ranking));
        }
        else
        {
            // todo: log the unsuccessful finish reason somewhere for the user to see why this translation failed?
            // expected reasons to get here are "content_filter" or empty response
        }
    }

    [DataMember(Name = "AuthenticationKey")]
    public string? SerializedAuthenticationKey
    {
        get => SaveCredentials ? Credentials[0].Value : null;
        set => Credentials[0].Value = value;
    }

    // this translator is currently adapted to work the best with "gpt-3.5-turbo-instruct", "gpt-3.5-turbo" or "gpt-4-turbo"
    [DataMember(Name = "ModelName")]
    public string? ModelName
    {
        get => ExpandModelNameAliases(Credentials[1].Value);
        set => Credentials[2].Value = value;
    }

    private string? AuthenticationKey => Credentials[0].Value;

    private static string? ExpandModelNameAliases(string? modelName)
    {
        // expand alternative model names to known model names

        return modelName switch
        {
            "gpt-35-turbo" => "gpt-3.5-turbo",
            "gpt-35-turbo-instruct" => "gpt-3.5-turbo-instruct",
            _ => modelName,
        };
    }

    private static IList<ICredentialItem> GetCredentials()
    {
        return
        [
            new CredentialItem("AuthenticationKey", "Key"),
            new CredentialItem("ModelName", "Model Name", false),
        ];
    }
}
