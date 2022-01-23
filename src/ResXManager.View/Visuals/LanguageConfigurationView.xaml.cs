namespace ResXManager.View.Visuals;

using System;
using System.ComponentModel;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using ResXManager.Infrastructure;
using ResXManager.View.Tools;

using TomsToolbox.Composition;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf.Composition;
using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for LanguageConfigurationView.xaml
/// </summary>
[DataTemplate(typeof(LanguageConfigurationViewModel))]
public partial class LanguageConfigurationView
{
    [ImportingConstructor]
    public LanguageConfigurationView(IExportProvider exportProvider)
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

    private void Language_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
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
    public override object? GroupNameFromItem(object? item, int level, CultureInfo culture)
    {
        var cultureItem = item as CultureInfo;

        return cultureItem?.GetAncestors().LastOrDefault();
    }
}
