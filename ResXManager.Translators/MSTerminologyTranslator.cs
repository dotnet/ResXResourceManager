namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.Microsoft.TerminologyService;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

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

        public override void Translate(ITranslationSession translationSession)
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

                    try
                    {
                        var response = client.GetTranslations(item.Source, translationSession.SourceLanguage.Name,
                            targetCulture.Name, SearchStringComparison.CaseInsensitive, SearchOperator.Contains,
                            translationSources, false, 5, false, null);

                        if (response != null)
                        {
                            var matches = response
                                .SelectMany(match => match?.Translations?.Select(trans => new TranslationMatch(this, trans?.TranslatedText, match.ConfidenceLevel / 100.0)))
                                .Where(m => m?.TranslatedText != null)
                                .Distinct(TranslationMatch.TextComparer);

                            translationSession.Dispatcher.BeginInvoke(() =>
                            {
                                item.Results.AddRange(matches);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        translationSession.AddMessage(DisplayName + ": " + ex.Message);
                        break;
                    }
                }
            }
        }
    }
}