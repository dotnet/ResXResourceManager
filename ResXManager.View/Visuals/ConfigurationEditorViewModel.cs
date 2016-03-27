namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport("Content", Sequence = 3)]
    class ConfigurationEditorViewModel : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public ConfigurationEditorViewModel(ResourceManager resourceManager, Configuration configuration)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
        }

        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);
                return _resourceManager;
            }
        }

        public Configuration Configuration
        {
            get
            {
                Contract.Ensures(Contract.Result<Configuration>() != null);
                return _configuration;
            }
        }

        public ICommand SortNodesByKeyCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(SortNodesByKey);
            }
        }

        private void SortNodesByKey()
        {
            foreach (var language in _resourceManager.ResourceEntities.SelectMany(entity => entity.Languages).Distinct().ToArray())
            {
                Contract.Assume(language != null);
                language.SortNodesByKey();
            }
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Configuration;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
