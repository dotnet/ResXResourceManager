namespace tomenglertde.ResXManager.View.Visuals
{
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport("Content", Sequence=4)]
    class LanguageConfigurationViewModel : ObservableObject
    {
        public override string ToString()
        {
            return Resources.ShellTabHeader_Languages;
        }
    }
}
