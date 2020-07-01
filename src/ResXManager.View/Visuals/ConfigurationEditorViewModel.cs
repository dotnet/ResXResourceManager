namespace ResXManager.View.Visuals
{
    using System.Composition;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Properties;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 3)]
    [Shared]
    internal class ConfigurationEditorViewModel : ObservableObject
    {
        [ImportingConstructor]
        public ConfigurationEditorViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration)
        {
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
                .ForEach(language => language.SortNodes(Configuration.ResXSortingComparison, Configuration.DuplicateKeyHandling));
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Configuration;
        }
    }
}
