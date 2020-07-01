namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.MoveToResource)]
    [VisualCompositionExport(RegionId.Configuration)]
    [Shared]
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
