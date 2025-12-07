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
using Newtonsoft.Json.Linq;
using ResXManager.Infrastructure;
using TomsToolbox.Essentials;
using JsonConvert = Newtonsoft.Json.JsonConvert;

#pragma warning disable CA1305
#pragma warning disable IDE0057
#pragma warning disable CA1865

[Export(typeof(ITranslator)), Shared]
public class OpenAITranslator : TranslatorBase
{
    private HttpClient _sharedClient = new();
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private TiktokenTokenizer? _cachedTokenizer;
    private DateTime _minuteStartTime = DateTime.UtcNow;
    private SemaphoreSlim? _throttler;
    private int _tokenPerMinute;
    private int _tokensUsedThisMinute;

    public OpenAITranslator()
        : base(
            "OpenAI", "OpenAI",
            new Uri("https://openai.com/api/"),
            GetCredentials()
        )
    {
    }

    [DataMember]
    public bool CountTokens { get; set; } = true;

    [DataMember]
    public string CustomPrompt { get; set; } = "";

    [DataMember]
    public bool IncludeCommentsInPrompt { get; set; } = true;

    [DataMember]
    public int MaxParallelRequests { get; set; } = 10;

    [DataMember]
    public int MaxTokens { get; set; } = 4096;

    [DataMember(Name = "ModelName")]
    public string? ModelName
    {
        get => ExpandModelNameAliases(Credentials[2].Value);
        set => Credentials[2].Value = value;
    }

    [DataMember(Name = "AuthenticationKey")]
    public string? SerializedAuthenticationKey
    {
        get => SaveCredentials ? Credentials[0].Value : null;
        set => Credentials[0].Value = value;
    }

    [DataMember]
    public float Temperature { get; set; }

    [DataMember(Name = "Url")]
    public string? Url
    {
        get => Credentials[1].Value;
        set => Credentials[1].Value = value;
    }

    private string? AuthenticationKey => Credentials[0].Value;
    private int CompletionTokens => MaxTokens - PromptTokens;
    private int PromptTokens => MaxTokens / 2;

    public async Task<int?> GetTokenLimitPerMinuteAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = ModelName,
            input = "ping",
        };

        using var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await client.PostAsync(
                "responses ",
                content,
                cancellationToken
            ).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.Headers.TryGetValues("x-ratelimit-limit-tokens", out var values))
            {
                var value = values.FirstOrDefault();
                if (int.TryParse(value, out var limit))
                {
                    return limit;
                }
            }
        }
        catch (HttpRequestException)
        {
            // Fallback to conservative limit
            return 90000;
        }

        return null;
    }

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

        ConfigureSharedHttpClient();

        try
        {
            _sharedClient.BaseAddress = new Uri(Url, UriKind.Absolute);
        }
        catch (Exception e) when (e is ArgumentNullException or ArgumentException or UriFormatException)
        {
            translationSession.AddMessage("OpenAI Translator requires valid resource endpoint URL.");
            return;
        }

        // Initialize token limit
        var tpm = await GetTokenLimitPerMinuteAsync(_sharedClient, translationSession.CancellationToken);
        if (tpm == null)
        {
            translationSession.AddMessage("OpenAI Translator response with no token per minutes");
            return;
        }

        _tokenPerMinute = (int)tpm;
        _minuteStartTime = DateTime.UtcNow;
        _tokensUsedThisMinute = 0;

        await TranslateUsingCompletionsModelParallel(translationSession, _sharedClient).ConfigureAwait(false);
    }

    private static string? ExpandModelNameAliases(string? modelName)
    {
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

    private void ConfigureSharedHttpClient()
    {
        var handler = new HttpClientHandler
        {
            MaxConnectionsPerServer = MaxParallelRequests
        };

        _sharedClient = new HttpClient(handler);
        _sharedClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _sharedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthenticationKey}");
        _sharedClient.Timeout = TimeSpan.FromMinutes(5);
    }

    private string? GenerateCompletionModelPromptForTranslation(ITranslationSession translationSession, ITranslationItem translationItem)
    {
        var neutralResourcesLanguage = translationSession.NeutralResourcesLanguage;
        var allItems = translationItem.GetAllItems(neutralResourcesLanguage);

        if (!allItems.Any())
            return null;

        var targetCulture = (translationItem.TargetCulture.Culture ?? neutralResourcesLanguage).Name;

        var promptBuilder = new StringBuilder();
        promptBuilder.Append($"You are a professional translator fluent in all languages, able to understand and convey both literal and nuanced meanings. You are an expert in the target language \"{targetCulture}\", adapting the style and tone to different types of texts.\n");

        if (!CustomPrompt.IsNullOrWhiteSpace())
        {
            promptBuilder.Append(CustomPrompt);
            promptBuilder.Append('\n');
        }

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

        promptBuilder.Append($"Here is a list of words or sentences with the same meaning in different languages. Please translate the following word or sentence into \"{targetCulture}\" only. Do not include any language codes, labels, or extra text. Provide only the translation.\n");
        promptBuilder.Append("TRANSLATIONS:\n");

        allItems.Select(s => $"{s.Culture.Name}: {s.Text}\n")
            .ForEach(s => promptBuilder.Append(s));

        return promptBuilder.ToString();
    }

    private IEnumerable<(ITranslationItem item, string prompt, int estimatedTokens)> PackCompletionModelPrompts(
        ITranslationSession translationSession,
        TiktokenTokenizer? tokenizer)
    {
        foreach (var item in translationSession.Items)
        {
            var prompt = GenerateCompletionModelPromptForTranslation(translationSession, item);
            if (prompt is null)
            {
                translationSession.AddMessage($"No prompt were generated for resource: {item.Source.Substring(0, Math.Min(20, item.Source.Length))}...");
                continue;
            }

            var estimatedTokens = 0;
            if (tokenizer is not null)
            {
                estimatedTokens = tokenizer.CountTokens(prompt);
                if (estimatedTokens > PromptTokens)
                {
                    translationSession.AddMessage($"Prompt for resource would exceed {PromptTokens} tokens: {item.Source.Substring(0, Math.Min(20, item.Source.Length))}...");
                    continue;
                }
            }

            yield return (item, prompt, estimatedTokens);
        }
    }

    private async Task ProcessSingleBatch(
        (ITranslationItem item, string prompt, int estimatedTokens) batch,
        ITranslationSession translationSession,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var retries = 0;
        const int maxRetries = 5;

        while (!cancellationToken.IsCancellationRequested && retries < maxRetries)
        {
            // Rate Limiting Request
            await WaitForTokenAvailability(batch.estimatedTokens, cancellationToken).ConfigureAwait(false);

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

            try
            {
                var completionsResponse = await client.PostAsync(
                   "responses",
                    content,
                    cancellationToken
                ).ConfigureAwait(false);

                if (completionsResponse.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    var backOffSeconds = Math.Min(1 << retries, 60); // Maximum 60 seconds
                    retries++;
                    await Task.Delay(backOffSeconds * 1000, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                completionsResponse.EnsureSuccessStatusCode();

                var responseContent = await completionsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var root = JToken.Parse(responseContent);

                var text =
                    root["output"]?
                        .First?["content"]?
                        .First?["text"]?
                        .ToString();

                if (string.IsNullOrWhiteSpace(text))
                    text = string.Empty;

                await translationSession.MainThread.StartNew(() =>
                    batch.item.Results.Add(new TranslationMatch(this, text, Ranking)),
                    cancellationToken
                ).ConfigureAwait(false);

                // Update token consumption
                UpdateTokenUsage(batch.estimatedTokens + CompletionTokens);

                break; // Success, out of the retry loop
            }
            catch (HttpRequestException ex)
            {
                retries++;
                if (retries >= maxRetries)
                {
                    translationSession.AddMessage($"Failed after {maxRetries} retries: {ex.Message}");
                    break;
                }
                await Task.Delay((1 << retries) * 1000, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task TranslateUsingCompletionsModelParallel(ITranslationSession translationSession, HttpClient client)
    {
        if (CountTokens && _cachedTokenizer == null)
        {
            _cachedTokenizer = TryCreateTokenizerForModel(ModelName);
        }

        // Initialise or update throttlers if MaxParallelRequests has changed
        if (_throttler == null || _throttler.CurrentCount != MaxParallelRequests)
        {
            _throttler?.Dispose();
            _throttler = new SemaphoreSlim(MaxParallelRequests, MaxParallelRequests);
        }

        var cancellationToken = translationSession.CancellationToken;
        var batches = PackCompletionModelPrompts(translationSession, _cachedTokenizer).ToList();

        // Parallel processing with SemaphoreSlim for control
        var tasks = new List<Task>();
        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (string.IsNullOrWhiteSpace(batch.item.Source) || (batch.item.Source.Length == 1 && batch.item.Source.Contains("\u200B")))
                continue;

            await _throttler.WaitAsync(cancellationToken).ConfigureAwait(false);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ProcessSingleBatch(batch, translationSession, client, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _throttler.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private TiktokenTokenizer? TryCreateFallbackTokenizer()
    {
        try
        {
            // o200k_base is the latest encoding (used by GPT-4o, GPT-4o-mini, etc.)
            return TiktokenTokenizer.CreateForEncoding("o200k_base");
        }
        catch
        {
            // If that also fails, we return zero.
            // The code then works without token counting.
            return null;
        }
    }

    private TiktokenTokenizer? TryCreateTokenizerForModel(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return TryCreateFallbackTokenizer();
        }

        try
        {
            // First, try to create the tokeniser for the specific model.
#pragma warning disable CS8604 // Possible null reference argument.
            return TiktokenTokenizer.CreateForModel(modelName);
#pragma warning restore CS8604 // Possible null reference argument.
        }
        catch
        {
            // If the model is not supported, use fallback
            return TryCreateFallbackTokenizer();
        }
    }

    private void UpdateTokenUsage(int tokens)
    {
        _rateLimitSemaphore.Wait();
        try
        {
            _tokensUsedThisMinute += tokens;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private async Task WaitForTokenAvailability(int requiredTokens, CancellationToken cancellationToken)
    {
        if (_tokenPerMinute <= 0)
            return;

        await _rateLimitSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Reset counter when one minute has elapsed
            var now = DateTime.UtcNow;
            if ((now - _minuteStartTime).TotalMinutes >= 1)
            {
                _minuteStartTime = now;
                _tokensUsedThisMinute = 0;
            }

            // Wait when limit is reached
            System.Diagnostics.Debug.WriteLine($"_tokensUsedThisMinute {_tokensUsedThisMinute} from _tokenPerMinute {_tokenPerMinute} ");
            while (_tokensUsedThisMinute + requiredTokens > _tokenPerMinute)
            {
                System.Diagnostics.Debug.WriteLine($"Token Limit ");
                var waitTime = _minuteStartTime.AddMinutes(1) - DateTime.UtcNow;
                if (waitTime.TotalMilliseconds > 0)
                {
                    await Task.Delay(Math.Min((int)waitTime.TotalMilliseconds, 5000), cancellationToken).ConfigureAwait(false);
                }

                // Reset after waiting period
                now = DateTime.UtcNow;
                if ((now - _minuteStartTime).TotalMinutes >= 1)
                {
                    _minuteStartTime = now;
                    _tokensUsedThisMinute = 0;
                }
            }
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}
