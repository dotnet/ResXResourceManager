namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
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
    }

    public class CultureInfoGroupDescription : GroupDescription
    {
        [CanBeNull]
        public override object GroupNameFromItem([CanBeNull] object item, int level, CultureInfo culture)
        {
            var cultureItem = item as CultureInfo;

            return cultureItem?.GetAncestors().LastOrDefault();
        }
    }
}
