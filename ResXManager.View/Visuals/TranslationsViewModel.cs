namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Translators;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Content, Sequence = 2)]
    internal class TranslationsViewModel : ObservableObject
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

        public Translations Translations => _translations;

        public ResourceManager ResourceManager => _resourceManager;

        public Configuration Configuration => _configuration;

        public IEnumerable<ITranslator> Translators => _translatorHost.Translators;

        public override string ToString() => Resources.ShellTabHeader_Translate;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_translatorHost != null);
            Contract.Invariant(_translations != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
