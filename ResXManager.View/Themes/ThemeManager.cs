namespace ResXManager.View.Themes
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf;

    [Export]
    public class ThemeManager : ObservableObject
    {
        public bool IsDarkTheme { get; set; }
    }
}
