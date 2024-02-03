namespace ResXManager;

using System.Composition;

using ResXManager.Infrastructure;
using ResXManager.Properties;

using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition.AttributedModel;

[LocalizedDisplayName(StringResourceKey.ColorTheme_Title)]
[VisualCompositionExport(RegionId.Configuration)]
[Shared]
internal class ColorThemeConfigurationViewModel : ObservableObject
{
    public ColorThemeConfigurationViewModel(IStandaloneConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IStandaloneConfiguration Configuration { get; }
}