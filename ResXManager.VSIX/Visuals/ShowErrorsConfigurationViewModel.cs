namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

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
