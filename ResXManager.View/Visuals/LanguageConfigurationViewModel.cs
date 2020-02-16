namespace ResXManager.View.Visuals
{
    using ResXManager.Infrastructure;
    using ResXManager.View.Properties;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.Mef;

    [VisualCompositionExport(RegionId.Content, Sequence = 4)]
    internal class LanguageConfigurationViewModel : ObservableObject
    {
        public override string ToString()
        {
            return Resources.ShellTabHeader_Languages;
        }
    }
}
