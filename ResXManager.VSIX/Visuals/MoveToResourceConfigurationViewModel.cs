namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    [LocalizedDisplayName(StringResourceKey.MoveToResource)]
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
