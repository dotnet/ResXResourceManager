namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.ServiceModel;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.Microsoft.TerminologyService;

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
            : base("MSTerm", "MS Terminology", _uri, new ICredentialItem[0])
        {
        }

        public override void Translate(ITranslationSession translationSession)
        {
            using (var client = new TerminologyClient(_binding, _endpoint))
            {
                var translationSources = new TranslationSources() {TranslationSource.UiStrings};
                foreach (var item in translationSession.Items)
                {
                    if (translationSession.IsCanceled)
                        break;

                    Contract.Assume(item != null);

                    var targetCulture = item.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;
                    if (targetCulture.IsNeutralCulture) targetCulture = CultureInfo.CreateSpecificCulture(targetCulture.Name);

                    try
                    {
                        var response = client.GetTranslations(item.Source, translationSession.SourceLanguage.Name,
                            targetCulture.Name, SearchStringComparison.CaseInsensitive, SearchOperator.Contains,
                            translationSources, false, 5, false, null);

                        if (response != null)
                        {
                            translationSession.Dispatcher.BeginInvoke(() =>
                            {
                                Contract.Requires(item != null);
                                Contract.Requires(response != null);

                                foreach (var match in response)
                                {
                                    Contract.Assume(match != null);
                                    foreach (var trans in match.Translations)
                                    {
                                        item.Results.Add(new TranslationMatch(this, trans.TranslatedText, match.ConfidenceLevel / 100.0));
                                    }
                                }
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