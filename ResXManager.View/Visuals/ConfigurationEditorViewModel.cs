namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Content, Sequence = 3)]
    internal class ConfigurationEditorViewModel : ObservableObject
    {
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public ConfigurationEditorViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
        }

        [NotNull]
        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);

                return _resourceManager;
            }
        }

        [NotNull]
        public Configuration Configuration
        {
            get
            {
                Contract.Ensures(Contract.Result<Configuration>() != null);

                return _configuration;
            }
        }

        [NotNull]
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
            _resourceManager.ResourceEntities
                .SelectMany(entity => entity.Languages)
                .ToArray()
                .ForEach(language => language.Save(_configuration.ResXSortingComparison));
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Configuration;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
