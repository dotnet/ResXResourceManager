namespace ResXManager.Translators
{
    using System;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    using ResXManager.Infrastructure;
    using ResXManager.Translators.Microsoft.TerminologyService;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [DataTemplate(typeof(MSTerminologyTranslator))]
    public class MSTerminologyTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator)), Shared]
    public class MSTerminologyTranslator : TranslatorBase
    {
        private static readonly BasicHttpBinding _binding = new(BasicHttpSecurityMode.Transport);
        private static readonly EndpointAddress _endpoint = new("https://api.terminology.microsoft.com/Terminology.svc");
        private static readonly Uri _uri = new("https://www.microsoft.com/en-us/language/default.aspx");

        public MSTerminologyTranslator()
            : base("MSTerm", "MS Terminology", _uri, Array.Empty<ICredentialItem>())
        {
        }

        protected override async Task Translate(ITranslationSession translationSession)
        {
            using (var client = new TerminologyClient(_binding, _endpoint))
            {
                var translationSources = new TranslationSources { TranslationSource.UiStrings };

                foreach (var item in translationSession.Items)
                {
                    if (translationSession.IsCanceled)
                        break;

                    var targetCulture = item.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;
                    if (targetCulture.IsNeutralCulture)
                    {
                        targetCulture = CultureInfo.CreateSpecificCulture(targetCulture.Name);
                    }

                    var response = await client.GetTranslationsAsync(
                        item.Source, translationSession.SourceLanguage.Name,
                        targetCulture.Name, SearchStringComparison.CaseInsensitive, SearchOperator.Contains,
                        translationSources, false, 5, false, null)
                        .ConfigureAwait(false);

                    if (response != null)
                    {
                        var matches = response
                            .SelectMany(match => match?.Translations?.Select(trans => new TranslationMatch(this, trans?.TranslatedText, Ranking * match.ConfidenceLevel / 100.0)) ?? Array.Empty<TranslationMatch>())
                            .Where(m => m?.TranslatedText != null)
                            .Distinct(TranslationMatch.TextComparer);

                        await translationSession.MainThread.StartNew(() => item.Results.AddRange(matches)).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}