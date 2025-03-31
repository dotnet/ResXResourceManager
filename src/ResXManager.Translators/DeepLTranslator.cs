namespace ResXManager.Translators;

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;
using TomsToolbox.Wpf.Composition.AttributedModel;

[DataTemplate(typeof(DeepLTranslator))]
public class DeepLTranslatorConfiguration : Decorator
{
}

[Export(typeof(ITranslator)), Shared]
public class DeepLTranslator() : TranslatorBase("DeepL", "DeepL", _uri, _credentialItems)
{
    private static readonly Uri _uri = new("https://deepl.com/translator");
    private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[]
    {
        new CredentialItem("APIKey", "API Key"),
        new CredentialItem("Url", "Api Url", false),
        new CredentialItem("GlossaryId", "Glossary Id", false)
    };

    [DataMember(Name = "ApiKey")]
    public string? SerializedApiKey
    {
        get => SaveCredentials ? Credentials[0].Value : null;
        set => Credentials[0].Value = value;
    }

    [DataMember(Name = "ApiUrl")]
    public string? ApiUrl
    {
        get => Credentials[1].Value;
        set => Credentials[1].Value = value;
    }


    [DataMember(Name = "GlossaryId")]
    public string? GlossaryId
    {
        get => Credentials[2].Value;
        set => Credentials[2].Value = value;
    }

    private string? ApiKey => Credentials[0].Value;

    protected override async Task Translate(ITranslationSession translationSession)
    {
        if (ApiKey.IsNullOrEmpty())
        {
            translationSession.AddMessage("DeepL Translator requires API Key.");
            return;
        }

        var targetLanguages = translationSession.Items.GroupBy(item => item.TargetCulture);
        var targetLangCount = targetLanguages.Count();

        if (!GlossaryId.IsNullOrWhiteSpace() && targetLangCount >= 2)
        {
            translationSession.AddMessage("A glossary id can only be used with a single target language.");
            return;
        }

        foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
        {
            if (translationSession.IsCanceled)
                break;

            var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

            using var itemsEnumerator = languageGroup.GetEnumerator();
            while (true)
            {
                var sourceItems = itemsEnumerator.Take(10);
                if (translationSession.IsCanceled || !sourceItems.Any())
                    break;

                var model = new DeepLTranslationModel()
                {
                    GlossaryId = GlossaryId,
                    SourceLang = DeepLLangCode(translationSession.SourceLanguage),
                    TargetLang = DeepLLangCode(targetCulture),
                    Text = sourceItems.Select(item => RemoveKeyboardShortcutIndicators(item.Source)).ToArray()
                };

                var apiUrl = ApiUrl;
                if (apiUrl.IsNullOrWhiteSpace())
                {
                    apiUrl = "https://api.deepl.com/v2/translate";
                }

                // Call the DeepL API
                var response = await GetHttpResponse<TranslationRootObject>(
                    apiUrl,
                    model,
                    ApiKey,
                    translationSession.CancellationToken)
                    .ConfigureAwait(false);

                await translationSession.MainThread.StartNew(() =>
                {
                    foreach (var (translationItem, text) in sourceItems.Zip(response.Translations ?? [],
                                 (a, b) => new Tuple<ITranslationItem, string?>(a, b.Text)))
                    {
                        translationItem.Results.Add(new TranslationMatch(this, text, Ranking));
                    }
                }).ConfigureAwait(false);
            }
        }
    }

    private static string DeepLLangCode(CultureInfo cultureInfo)
    {
        var iso1 = cultureInfo.TwoLetterISOLanguageName;
        return iso1;
    }

    /// <summary>
    /// Sending a POST Request to <paramref name="baseUrl"/>
    /// with the HttpContent of <paramref name="model"/> as JSON.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="baseUrl"></param>
    /// <param name="model"></param>
    /// <param name="apiKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns the Translation result.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<T> GetHttpResponse<T>(string? baseUrl, DeepLTranslationModel model, string? apiKey, CancellationToken cancellationToken)
        where T : class
    {
        using var httpClient = new HttpClient();

        var json = JsonConvert.SerializeObject(model);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Authorization = new("DeepL-Auth-Key", apiKey);

        var response = await httpClient.PostAsync(new Uri(baseUrl), content, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods => not available in NetFramework
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return JsonConverter<T>(stream) ?? throw new InvalidOperationException("Empty response.");
    }

    private static T? JsonConverter<T>(Stream stream)
        where T : class
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }

    [DataContract]
    private sealed class Translation
    {
        [DataMember(Name = "text")]
        public string? Text { get; set; }
    }

    [DataContract]
    private sealed class TranslationRootObject
    {
        [DataMember(Name = "translations")]
        public Translation[]? Translations { get; set; }
    }

    /// <summary>
    /// Model for translating with DeepL.
    /// For POST Request as of March 14, 2025.
    /// </summary>
    [DataContract]
    private class DeepLTranslationModel
    {
        [DataMember(Name = "text")]
        public string[]? Text { get; set; }

        [DataMember(Name = "target_lang")]
        public string? TargetLang { get; set; }

        [DataMember(Name = "source_lang")]
        public string? SourceLang { get; set; }

        [DataMember(Name = "glossary_id")]
        public string? GlossaryId { get; set; }
    }
}
