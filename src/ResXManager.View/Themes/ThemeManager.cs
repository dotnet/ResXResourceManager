namespace ResXManager.View.Themes
{
    using System.ComponentModel;
    using System.Composition;

    using TomsToolbox.Wpf;

    [Export, Shared]
    public partial class ThemeManager : INotifyPropertyChanged
    {
        public bool IsDarkTheme { get; set; }
    }
}
