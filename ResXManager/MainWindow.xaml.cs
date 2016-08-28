namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainWindow
    {
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public MainWindow(ExportProvider exportProvider, ITracer tracer)
        {
            Contract.Requires(exportProvider != null);

            _tracer = tracer;

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();

                AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            var resourceManager = this.GetExportProvider().GetExportedValue<ResourceManager>();

            if (!resourceManager.HasChanges)
                return;

            switch (MessageBox.Show("Do you want to save the changes?", "Resource Manager", MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;

                case MessageBoxResult.No:
                    break;

                case MessageBoxResult.Yes:
                    try
                    {
                        resourceManager.Save();
                    }
                    catch (Exception ex)
                    {
                        _tracer.TraceError(ex.ToString());
                        MessageBox.Show(ex.Message);
                        e.Cancel = true;
                    }
                    break;
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

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}