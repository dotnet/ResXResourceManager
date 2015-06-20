namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition.Hosting;
    using System.IO;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly ICompositionHost _compositionHost = new CompositionHost();

        public App()
        {
#if DEBUG
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
#endif
        }

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            _compositionHost.AddCatalog(new DirectoryCatalog(Path.GetDirectoryName(GetType().Assembly.Location), "ResXManager.*.dll"));
            
            ExportProviderLocator.Register(_compositionHost.Container);
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            _compositionHost.Dispose();

            base.OnExit(e);
        }
    }
}
