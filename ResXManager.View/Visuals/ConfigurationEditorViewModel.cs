namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
        [ImportingConstructor]
        public ConfigurationEditorViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);

            ResourceManager = resourceManager;
            Configuration = configuration;
        }

        [NotNull]
        public ResourceManager ResourceManager { get; }

        [NotNull]
        public Configuration Configuration { get; }

        [NotNull]
        public ICommand SortNodesByKeyCommand => new DelegateCommand(SortNodesByKey);

        private void SortNodesByKey()
        {
            ResourceManager.ResourceEntities
                .SelectMany(entity => entity.Languages)
                .ToArray()
                .ForEach(language => language.Save(Configuration.ResXSortingComparison));
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Configuration;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(ResourceManager != null);
            Contract.Invariant(Configuration != null);
        }
    }
}
