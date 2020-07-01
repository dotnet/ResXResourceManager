namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.ShowErrorsConfiguration_Header)]
    [VisualCompositionExport(RegionId.Configuration)]
    [Shared]
    internal class ShowErrorsConfigurationViewModel
    {
        [ImportingConstructor]
        public ShowErrorsConfigurationViewModel([NotNull] DteConfiguration configuration)
        {
            Configuration = configuration;
        }

        [NotNull]
        public DteConfiguration Configuration { get; }
    }
}
