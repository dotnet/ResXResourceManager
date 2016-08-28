namespace tomenglertde.ResXManager.View.Visuals
{
    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Content, Sequence = 4)]
    internal class LanguageConfigurationViewModel : ObservableObject
    {
        public override string ToString()
        {
            return Resources.ShellTabHeader_Languages;
        }
    }
}
