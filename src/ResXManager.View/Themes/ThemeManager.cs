namespace ResXManager.View.Themes;

using System.Composition;

using TomsToolbox.Wpf;

[Export, Shared]
public class ThemeManager : ObservableObject
{
    public bool IsDarkTheme { get; set; }
}
