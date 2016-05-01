namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using TomsToolbox.Desktop.Composition;
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
        public MainWindow(ICompositionHost compositionHost)
        {
            this.SetExportProvider(compositionHost.Container);

            InitializeComponent();

            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));
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
                if (link == null)
                    return;

                var navigateUri = link.NavigateUri;
                if (navigateUri == null)
                    return;

                url = navigateUri.ToString();
            }

            Process.Start(url);
        }
    }
}