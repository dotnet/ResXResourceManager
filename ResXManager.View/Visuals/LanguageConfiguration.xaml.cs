namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Input;
    using tomenglertde.ResXManager.View.Tools;

    /// <summary>
    /// Interaction logic for FlagConfiguration.xaml
    /// </summary>
    public partial class LanguageConfiguration
    {
        public LanguageConfiguration()
        {
            InitializeComponent();
        }

        private void Language_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var control = sender as FrameworkElement;
            if (control == null)
                return;

            var specificCulture = control.DataContext as CultureInfo;
            if (specificCulture == null)
                return;

            var neutralCulture = specificCulture.Parent;

            NeutralCultureCountyOverrides.Default[neutralCulture] = specificCulture;
            ListBox.Items.Refresh();
        }
    }
}
