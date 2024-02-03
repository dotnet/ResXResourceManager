namespace ResXManager.View.Visuals;

using System;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

using ResXManager.Infrastructure;

using TomsToolbox.Composition;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition;
using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for ShellView.xaml
/// </summary>
[Export]
[DataTemplate(typeof(ShellViewModel))]
public partial class ShellView
{
    [ImportingConstructor]
    public ShellView(IExportProvider exportProvider)
    {
        try
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();

            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }
        catch (Exception ex)
        {
            exportProvider.TraceXamlLoaderError(ex);
        }
    }

    private void OpenSourceOverlay_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Children.Clear();
        }
    }

    private void OpenSourceOverlay_Loaded(object? sender, RoutedEventArgs e)
    {
        if (Properties.Settings.Default.IsOpenSourceMessageConfirmed)
        {
            if (sender is Grid grid)
            {
                grid.Children.Clear();
            }
        }
    }

    private void OpenSourceOverlayTextContainer_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!(sender is Decorator container))
            return;

        container.BeginInvoke(() =>
        {
            if (!container.IsLoaded)
                return;

            try
            {
                var xaml = Properties.Resources.OpenSourceOverlay_Message;

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    container.Child = (UIElement)XamlReader.Load(stream);
                }
            }
            catch
            {
                // just go with the english default
            }
        });
    }
}