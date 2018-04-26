namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for LanguageConfigurationView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageConfigurationViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LanguageConfigurationView
    {
        [ImportingConstructor]
        public LanguageConfigurationView([NotNull] ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }

        private void Language_MouseDoubleClick([NotNull] object sender, [NotNull] MouseButtonEventArgs e)
        {
            var specificCulture = (sender as FrameworkElement)?.DataContext as CultureInfo;
            if (specificCulture == null)
                return;

            var neutralCulture = specificCulture.Parent;

            NeutralCultureCountryOverrides.Default[neutralCulture] = specificCulture;
            ListBox.Items.Refresh();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
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

            return cultureItem?.GetAncestors().LastOrDefault();
        }
    }
}
