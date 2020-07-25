namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.ShowErrorsConfiguration_Header)]
    [VisualCompositionExport(RegionId.Configuration)]
    [Shared]
    internal class ShowErrorsConfigurationViewModel
    {
        [ImportingConstructor]
        public ShowErrorsConfigurationViewModel(DteConfiguration configuration)
        {
            Configuration = configuration;
        }

        public DteConfiguration Configuration { get; }
    }
}
