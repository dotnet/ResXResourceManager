namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Input;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Core;

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
            var specificCulture = sender.Maybe()
                .Select(x => x as FrameworkElement)
                .Return(x => x.DataContext as CultureInfo);

            if (specificCulture == null)
                return;

            var neutralCulture = specificCulture.Parent;
            Contract.Assume(neutralCulture != null);

            NeutralCultureCountyOverrides.Default[neutralCulture] = specificCulture;
            ListBox.Items.Refresh();
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(ListBox != null);
        }

    }
}
