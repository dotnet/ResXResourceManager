namespace ResXManager.Translators;

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using global::Microsoft.ML.Tokenizers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResXManager.Infrastructure;
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
    // whether to count the number of tokens sent. Users can disable token counting to use models not supported by the TiktokenTokenizer.
    public bool CountTokens { get; set; } = true;

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

        var tpm = await GetTokenLimitPerMinuteAsync(client, ModelName, translationSession.CancellationToken);

        if (tpm == null)
        {
            translationSession.AddMessage("OpenAI Translator response with no token per minutes");
            return;
        }

        _tokenPerMinute = (int)tpm;

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
        TiktokenTokenizer? tokenizer = null;
        if (CountTokens)
        {
            tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");
        }

        var retries = 0;

        var cancellationToken = translationSession.CancellationToken;

        foreach (var batch in PackCompletionModelPrompts(translationSession, tokenizer))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var requestBody = new
                {
                    model = ModelName,            
                    input = batch.prompt,          
                    max_output_tokens = CompletionTokens,
                    temperature = Temperature
                };

                using var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var completionsResponse = await client.PostAsync(
                    "responses",
                    content,
                    cancellationToken
                ).ConfigureAwait(false);

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

                var root = JToken.Parse(responseContent);

                // safe lookup with null checking
                string? text =
                    root["output"]?
                        .First?["content"]?
                        .First?["text"]?
                        .ToString();

                // fallback if no text found
                if (string.IsNullOrWhiteSpace(text))
                    text = string.Empty;

                await translationSession.MainThread.StartNew(() => batch.item.Results.Add(new TranslationMatch(this, text, Ranking)), cancellationToken).ConfigureAwait(false);

                // break out of the retry loop
                break;
            }
        }
    }

    private IEnumerable<(ITranslationItem item, string prompt)> PackCompletionModelPrompts(ITranslationSession translationSession, TiktokenTokenizer? tokenizer)
    {
        foreach (var item in translationSession.Items)
        {
            var prompt = GenerateCompletionModelPromptForTranslation(translationSession, item);
            if (prompt is null)
            {
                translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, 20)}...");
                continue;
            }

            if (tokenizer is not null)
            {
                var tokens = tokenizer.CountTokens(prompt);
                if (tokens > PromptTokens)
                {
                    translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, 20)}...");
                    continue;
                }
            }

            yield return (item, prompt);
        }
    }

    private int _tokenPerMinute = 0;

    public async Task<int?> GetTokenLimitPerMinuteAsync(HttpClient client, string modelName, CancellationToken cancellationToken)
    {
        // Mini-Request, der kaum Tokens verbraucht
        var body = new
        {
            model = modelName,
            input = "ping",
            max_output_tokens = 1
        };

        using var content = new StringContent(
            Newtonsoft.Json.JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync(
            "https://api.openai.com/v1/responses",
            content,
            cancellationToken
        );

        // Fehler werfen, falls etwas schief geht
        response.EnsureSuccessStatusCode();

        // Header lesen
        if (response.Headers.TryGetValues("x-ratelimit-limit-tokens", out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out int limit))
            {
                return limit;
            }
        }

        // Fallback falls Header fehlt
        return null;
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

        promptBuilder.Append($"Here is a list of words or sentences with the same meaning in different languages. Please translate the following word or sentence into \"{targetCulture}\" only. Do not provide any additional information.\n");
        promptBuilder.Append("TRANSLATIONS:\n");

        // add all existing translations to prompt
        allItems.Select(s => $"{s.Culture.Name}: {s.Text}\n")
            .ForEach(s => promptBuilder.Append(s));

        // the target language is the last language in the prompt, note that the prompt must not end with a space due to tokenization issues
        promptBuilder.Append($"{targetCulture}:");

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

    // this translator is currently adapted to work the best with "gpt-3.5-turbo-instruct", "gpt-3.5-turbo" or "gpt-4-turbo"
    [DataMember(Name = "ModelName")]
    public string? ModelName
    {
        get => ExpandModelNameAliases(Credentials[2].Value);
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
            new CredentialItem("Url", "Endpoint Url", false) { Value = "https://api.openai.com/v1/" },
            new CredentialItem("ModelName", "Model Name", false),
        ];
    }
}
