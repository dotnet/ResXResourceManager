namespace ResXManager.VSIX.Visuals;

using System.Composition;

using ResXManager.Infrastructure;
using ResXManager.VSIX.Compatibility;
using ResXManager.VSIX.Compatibility.Properties;

using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition.AttributedModel;

[LocalizedDisplayName(StringResourceKey.MoveToResource)]
[VisualCompositionExport(RegionId.Configuration)]
[Shared]
internal class MoveToResourceConfigurationViewModel : ObservableObject
{
    [ImportingConstructor]
    public MoveToResourceConfigurationViewModel(IDteConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IDteConfiguration Configuration { get; }
}
