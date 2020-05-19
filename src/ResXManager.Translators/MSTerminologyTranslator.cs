namespace ResXManager.Translators
{
    using System;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Translators.Microsoft.TerminologyService;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.Mef;

    [DataTemplate(typeof(MSTerminologyTranslator))]
    public class MSTerminologyTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator))]
    public class MSTerminologyTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly BasicHttpBinding _binding = new BasicHttpBinding();
        [NotNull]
        private static readonly EndpointAddress _endpoint = new EndpointAddress("http://api.terminology.microsoft.com/Terminology.svc");
        [NotNull]
        private static readonly Uri _uri = new Uri("https://www.microsoft.com/en-us/language/default.aspx");

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
                            .SelectMany(match => match?.Translations?.Select(trans => new TranslationMatch(this, trans?.TranslatedText, Ranking * match.ConfidenceLevel / 100.0)))
                            .Where(m => m?.TranslatedText != null)
                            .Distinct(TranslationMatch.TextComparer);

#pragma warning disable CS4014 // Because this call is not awaited ... => just push out results, no need to wait.
                        translationSession.MainThread.StartNew(() => item.Results.AddRange(matches));
                    }
                }
            }
        }
    }
}