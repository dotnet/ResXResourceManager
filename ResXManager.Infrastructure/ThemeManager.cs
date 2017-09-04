namespace tomenglertde.ResXManager.Infrastructure
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop;

    [Export]
    public class ThemeManager : ObservableObject
    {
        public bool IsDarkTheme { get; set; }
    }
}
