namespace ResXManager.Translators;

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;

using JsonConvert = Infrastructure.JsonConvert;

/// <summary>
/// A translator implementation that uses the LibreTranslate API for translating resources.
/// </summary>
/// <remarks>
/// LibreTranslate is a free and open-source machine translation API.
/// This translator requires a URL to a LibreTranslate instance and optionally an API key.
/// </remarks>
[Export(typeof(ITranslator)), Shared]
public class LibreTranslateTranslator : TranslatorBase
{
    /// <summary>
    /// The default URI of the LibreTranslate instance.
    /// </summary>
    private static readonly Uri _uri = new("https://github.com/LibreTranslate/LibreTranslate");

    /// <summary>
    /// The credential items required by this translator (URL and optional API key).
    /// </summary>
    private static readonly IList<ICredentialItem> _credentialItems =
    [
        new CredentialItem("Url", "API Url", false),
        new CredentialItem("ApiKey", "API Key")
    ];

    /// <summary>
    /// Backing-field for <see cref="Alternatives"/>.
    /// </summary>
    private int _alternatives;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateTranslator"/> class.
    /// </summary>
    public LibreTranslateTranslator()
        : base("LibreTranslate", "LibreTranslate", _uri, _credentialItems)
    {
    }

    /// <summary>
    /// Gets or sets the serialized URL of the LibreTranslate instance.
    /// </summary>
    /// <remarks>
    /// This value is persisted only when <see cref="TranslatorBase.SaveCredentials"/> is <see langword="true"/>.
    /// </remarks>
    [DataMember(Name = "Url")]
    public string? SerializedUrl
    {
        get => SaveCredentials ? Credentials[0].Value : null;
        set => Credentials[0].Value = value;
    }

    /// <summary>
    /// Gets or sets the serialized API key for the LibreTranslate instance.
    /// </summary>
    /// <remarks>
    /// This value is persisted only when <see cref="TranslatorBase.SaveCredentials"/> is <see langword="true"/>.
    /// </remarks>
    [DataMember(Name = "ApiKey")]
    public string? SerializedApiKey
    {
        get => SaveCredentials ? Credentials[1].Value : null;
        set => Credentials[1].Value = value;
    }

    /// <summary>
    /// Gets or sets the serialized preferred number of alternative translations.
    /// </summary>
    [DataMember(Name = "Alternatives")]
    public int Alternatives
    {
        get => _alternatives;
        set
        {
            if (value == _alternatives)
            {
                return;
            }

            _alternatives = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the configured URL of the LibreTranslate instance.
    /// </summary>
    private string? Url => Credentials[0].Value;

    /// <summary>
    /// Gets the configured API key of the LibreTranslate instance.
    /// </summary>
    private string? ApiKey => Credentials[1].Value;

    /// <summary>
    /// Translates all items in the given translation session using the LibreTranslate API.
    /// </summary>
    /// <param name="translationSession">The translation session containing the items to translate.</param>
    /// <returns>A task that represents the asynchronous translate operation.</returns>
    protected override async Task Translate(ITranslationSession translationSession)
    {
        if (Url.IsNullOrEmpty())
        {
            translationSession.AddMessage("LibreTranslate Translator requires an API URL.");
            return;
        }

        foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
        {
            if (translationSession.IsCanceled)
                break;

            var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

            foreach (var item in languageGroup)
            {
                if (translationSession.IsCanceled)
                    break;

                var result = await TranslateAsync(
                    Url,
                    ApiKey,
                    RemoveKeyboardShortcutIndicators(item.Source),
                    translationSession.SourceLanguage,
                    targetCulture,
                    Alternatives,
                    translationSession.CancellationToken).ConfigureAwait(false);

                if (result is { Length: > 0 })
                {
                    await translationSession.MainThread.StartNew(() =>
                    {
                        for (var index = 0; index < result.Length; index++)
                        {
                            item.Results.Add(new TranslationMatch(this, result[index], Ranking));
                        }
                    }).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Translates text using the LibreTranslate API.
    /// </summary>
    /// <param name="url">The base URL of the LibreTranslate instance.</param>
    /// <param name="apiKey">The optional API key.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguage">The source language.</param>
    /// <param name="targetLanguage">The target language.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The translated text.</returns>
    public static async Task<string[]?> TranslateAsync(
        string url,
        string? apiKey,
        string text,
        CultureInfo sourceLanguage,
        CultureInfo targetLanguage,
        int? alternatives,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        var requestModel = new LibreTranslateRequest
        {
            Text = text,
            Source = sourceLanguage.TwoLetterISOLanguageName,
            Target = targetLanguage.TwoLetterISOLanguageName,
            ApiKey = apiKey,
            Alternatives = alternatives
        };

        var json = JsonConvert.SerializeObject(requestModel);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(new Uri(url), content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods => not available in NetFramework
        var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ParseResponse(jsonResponse);
    }

    /// <summary>
    /// Parses the LibreTranslate API response JSON and extracts the translated text.
    /// </summary>
    /// <param name="json">The JSON response from the LibreTranslate API.</param>
    /// <returns>The translated text, or <see langword="null"/> if parsing fails.</returns>
    public static string[]? ParseResponse(string json)
    {
        var result = JsonConvert.DeserializeObject<LibreTranslateResponse>(json);
        if (result?.TranslatedText is null)
        {
            return null;
        }

        return new[] { result.TranslatedText }.Concat(result.Alternatives ?? Array.Empty<string>()).ToArray();
    }

    /// <summary>
    /// Represents the request payload for the LibreTranslate `/translate` endpoint.
    /// </summary>
    private sealed class LibreTranslateRequest
    {
        /// <summary>
        /// Gets or sets the text to translate.
        /// </summary>
        [JsonProperty("q")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the source language code (ISO 639-1).
        /// </summary>
        [JsonProperty("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the target language code (ISO 639-1).
        /// </summary>
        [JsonProperty("target")]
        public string? Target { get; set; }

        /// <summary>
        /// Gets or sets the input format, e.g. &quot;text&quot; or &quot;html&quot;.
        /// </summary>
        [JsonProperty("format")]
        public string? Format { get; set; } = "text";

        /// <summary>
        /// Gets or sets the preferred number of alternative translations.
        /// </summary>
        [JsonProperty("alternatives")]
        public int? Alternatives { get; set; }

        /// <summary>
        /// Gets or sets the API key used to authenticate with the LibreTranslate instance.
        /// </summary>
        [JsonProperty("api_key", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? ApiKey { get; set; }
    }

    /// <summary>
    /// Represents the response payload returned by the LibreTranslate `/translate` endpoint.
    /// </summary>
    private sealed class LibreTranslateResponse
    {
        /// <summary>
        /// Gets or sets the translated text.
        /// </summary>
        [JsonProperty("translatedText")]
        public string? TranslatedText { get; set; }

        /// <summary>
        /// Gets or sets alternative translations, if provided by the API.
        /// </summary>
        [JsonProperty("alternatives")]
        public string[]? Alternatives { get; set; }
    }
}
