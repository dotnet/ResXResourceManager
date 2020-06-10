namespace ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Mef;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.XamlExtensions;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IDisposable
    {
        private readonly AggregateCatalog _compositionCatalog;
        private readonly CompositionContainer _compositionContainer;
        private readonly IExportProvider _exportProvider;

        public App()
        {
#if DEBUG && TEST_L10N
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
#endif

            _compositionCatalog = new AggregateCatalog();
            _compositionContainer = new CompositionContainer(_compositionCatalog, true);
            _exportProvider = new ExportProviderAdapter(_compositionContainer);
        }

        protected override void OnStartup([NotNull] StartupEventArgs e)
        {
            base.OnStartup(e);

            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);

#pragma warning disable CA2000 // Dispose objects before losing scope => AggregateCatalog will dispose all
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Infrastructure.Properties.AssemblyKey).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Model.Properties.AssemblyKey).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Translators.Properties.AssemblyKey).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(View.Properties.AssemblyKey).Assembly));
#pragma warning restore CA2000 // Dispose objects before losing scope

            _compositionContainer.ComposeExportedValue(_exportProvider);

            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_exportProvider));

            _compositionContainer.GetExportedValues<IService>().ForEach(service => service.Start());

            var tracer = _exportProvider.GetExportedValue<ITracer>();
            tracer.WriteLine("Started");

            tracer.WriteLine(ResXManager.Properties.Resources.IntroMessage);
            tracer.WriteLine(ResXManager.Properties.Resources.AssemblyLocation, folder ?? "unknown");
            tracer.WriteLine(ResXManager.Properties.Resources.Version, new AssemblyName(assembly.FullName).Version ?? new Version());

            VisualComposition.Error += (_, args) => tracer.TraceError(args.Text);

            MainWindow = _exportProvider.GetExportedValue<MainWindow>();
            MainWindow.Show();
        }

        protected override void OnExit([NotNull] ExitEventArgs e)
        {
            Dispose();

            base.OnExit(e);
        }

        public void Dispose()
        {
            _compositionCatalog.Dispose();
            _compositionContainer.Dispose();
        }
    }
}
