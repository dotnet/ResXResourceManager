namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

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

            NeutralCultureCountryOverrides.Default[neutralCulture] = specificCulture;
            ListBox.Items.Refresh();
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(ListBox != null);
        }
    }

    public class CultureInfoGroupDescription : GroupDescription
    {
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var cultureItem = item as CultureInfo;
            if (cultureItem == null)
                return null;

            return cultureItem.GetAncestors().LastOrDefault();
        }
    }
}
