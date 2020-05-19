namespace ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.Mef;

    [LocalizedDisplayName(StringResourceKey.MoveToResource)]
    [VisualCompositionExport(RegionId.Configuration)]
    internal class MoveToResourceConfigurationViewModel
    {
        [ImportingConstructor]
        public MoveToResourceConfigurationViewModel([NotNull] DteConfiguration configuration)
        {
            Configuration = configuration;
        }

        [NotNull]
        public DteConfiguration Configuration { get; }
    }
}
