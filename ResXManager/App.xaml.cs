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
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.Mef;
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

            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ResXManager.Infrastructure.ITracer).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ResXManager.Model.GlobalExtensions).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ResXManager.Translators.AzureTranslator).Assembly));
            _compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ResXManager.View.Appearance).Assembly));

            _compositionContainer.ComposeExportedValue(_exportProvider);

            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_exportProvider));

            _compositionContainer.GetExportedValues<IService>().ForEach(service => service.Start());

            var tracer = _exportProvider.GetExportedValue<ITracer>();
            tracer.WriteLine("Started");

            tracer.WriteLine(ResXManager.Properties.Resources.IntroMessage);
            tracer.WriteLine(ResXManager.Properties.Resources.AssemblyLocation, folder);
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
