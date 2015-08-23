namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Windows;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IDisposable
    {
        private readonly ICompositionHost _compositionHost = new CompositionHost();

#if DEBUG
        public App()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
        }
#endif

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var path = Path.GetDirectoryName(GetType().Assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(path));

            _compositionHost.AddCatalog(GetType().Assembly);
            _compositionHost.AddCatalog(new DirectoryCatalog(path, "ResXManager.*.dll"));

            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));
            ExportProviderLocator.Register(_compositionHost.Container);

            Trace.TraceInformation("Started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();

            base.OnExit(e);
        }

        public void Dispose()
        {
            _compositionHost.Dispose();
        }
    }
}
