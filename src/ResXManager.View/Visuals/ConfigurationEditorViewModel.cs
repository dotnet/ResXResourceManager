namespace ResXManager.View.Visuals
{
    using System.ComponentModel;
    using System.Composition;
    using System.Linq;
    using System.Windows.Input;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Properties;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 3)]
    [Shared]
    internal sealed partial class ConfigurationEditorViewModel : INotifyPropertyChanged
    {
        [ImportingConstructor]
        public ConfigurationEditorViewModel(ResourceManager resourceManager, IConfiguration configuration)
        {
            ResourceManager = resourceManager;
            Configuration = configuration;
        }

        public ResourceManager ResourceManager { get; }

        public IConfiguration Configuration { get; }

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
