namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.MoveToResource)]
    [VisualCompositionExport(RegionId.Configuration)]
    [Shared]
    internal class MoveToResourceConfigurationViewModel : ObservableObject
    {
        [ImportingConstructor]
        public MoveToResourceConfigurationViewModel(DteConfiguration configuration)
        {
            Configuration = configuration;
        }

        public DteConfiguration Configuration { get; }
    }
}
