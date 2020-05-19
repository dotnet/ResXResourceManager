namespace ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.Mef;

    [LocalizedDisplayName(StringResourceKey.ShowErrorsConfiguration_Header)]
    [VisualCompositionExport(RegionId.Configuration)]
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
