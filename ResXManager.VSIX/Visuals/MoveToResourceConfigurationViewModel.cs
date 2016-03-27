namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    [DisplayName("Move to resource")]
    [VisualCompositionExport("Configuration")]
    internal class MoveToResourceConfigurationViewModel
    {
        [ImportingConstructor]
        public MoveToResourceConfigurationViewModel(DteConfiguration configuration)
        {
            Configuration = configuration;
        }

        public DteConfiguration Configuration { get; }
    }
}
