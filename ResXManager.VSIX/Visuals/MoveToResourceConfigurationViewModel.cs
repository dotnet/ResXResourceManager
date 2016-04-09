namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    [LocalizedDisplayName(StringResourceKey.MoveToResource)]
    [VisualCompositionExport(RegionId.Configuration)]
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
