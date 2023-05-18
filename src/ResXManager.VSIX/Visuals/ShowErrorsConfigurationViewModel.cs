namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using ResXManager.Infrastructure;
    using ResXManager.VSIX.Compatibility;
    using ResXManager.VSIX.Properties;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.ShowErrorsConfiguration_Header)]
    [VisualCompositionExport(RegionId.Configuration)]
    [Shared]
    internal sealed class ShowErrorsConfigurationViewModel
    {
        [ImportingConstructor]
        public ShowErrorsConfigurationViewModel(IDteConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IDteConfiguration Configuration { get; }
    }
}
