namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainWindow
    {
        [ImportingConstructor]
        public MainWindow(ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();

                AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));
            }
            catch (Exception ex)
            {
                exportProvider.TraceError(ex.ToString());
            }
        }

        private static void Navigate_Click(object sender, RoutedEventArgs e)
        {
            string url;

            var source = e.OriginalSource as FrameworkElement;
            if (source != null)
            {
                var button = source.TryFindAncestorOrSelf<ButtonBase>();
                if (button == null)
                    return;

                url = source.Tag as string;
                if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return;
            }
            else
            {
                var link = e.OriginalSource as Hyperlink;

                var navigateUri = link?.NavigateUri;
                if (navigateUri == null)
                    return;

                url = navigateUri.ToString();
            }

            Process.Start(url);
        }
    }
}