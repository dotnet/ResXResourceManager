namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [Export]
    [DataTemplate(typeof(ShellViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ShellView
    {
        [ImportingConstructor]
        public ShellView([NotNull] ExportProvider exportProvider)
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

        private void OpenSourceOverlay_MouseDown([NotNull] object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).Children.Clear();
        }

        private void OpenSourceOverlay_Loaded([NotNull] object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.IsOpenSourceMessageConfirmed)
            {
                ((Grid)sender).Children.Clear();
            }
        }

        private void OpenSourceOverlayTextContainer_Loaded([NotNull] object sender, RoutedEventArgs e)
        {
            var container = (Decorator)sender;

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
                catch (Exception ex)
                {
                    // just go with the english default
                }
            });
        }
    }
}
