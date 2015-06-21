namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Translators;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport("Content", Sequence = 2)]
    class TranslationsViewModel : ObservableObject, IComposablePart
    {
        private readonly TranslatorHost _translatorHost;
        private readonly Translations _translations;
        private readonly ResourceManager _resourceManager;
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public TranslationsViewModel(TranslatorHost translatorHost, Translations translations, ResourceManager resourceManager, Configuration configuration)
        {
            Contract.Requires(translatorHost != null);
            Contract.Requires(translations != null);
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);

            _translatorHost = translatorHost;
            _translations = translations;
            _resourceManager = resourceManager;
            _configuration = configuration;
        }

        public Translations Translations
        {
            get
            {
                return _translations;
            }
        }

        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
            }
        }

        public Configuration Configuration
        {
            get
            {
                return _configuration;
            }
        }

        public IEnumerable<ITranslator> Translators
        {
            get
            {
                return _translatorHost.Translators;
            }
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Translate;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_translatorHost != null);
            Contract.Invariant(_translations != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
