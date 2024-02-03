namespace ResXManager.View.Themes;

using System.ComponentModel;
using System.Composition;

[Export, Shared]
public partial class ThemeManager : INotifyPropertyChanged
{
    public bool IsDarkTheme { get; set; }
}