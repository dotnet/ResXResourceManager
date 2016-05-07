namespace tomenglertde.ResXManager.Infrastructure
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop;

    [Export]
    public class ThemeManager : ObservableObject
    {
        private bool _isDarkTheme;

        public bool IsDarkTheme
        {
            get
            {
                return _isDarkTheme;
            }
            set
            {
                SetProperty(ref _isDarkTheme, value, nameof(IsDarkTheme));
            }
        }
    }
}
