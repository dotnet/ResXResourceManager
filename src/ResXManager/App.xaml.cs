namespace ResXManager
{
    using System;
    using System.Composition;
    using System.Composition.Hosting;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Mef2;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.XamlExtensions;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IDisposable
    {
        private CompositionHost? _container;

        public App()
        {
#if DEBUG && TEST_L10N
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
#endif
        }

        protected override void OnStartup([NotNull] StartupEventArgs e)
        {
            base.OnStartup(e);

            SynchronizationContextThrottle.TaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());

            var assembly = GetType().Assembly;

            var configuration = new ContainerConfiguration()
                .WithAssembly(assembly)
                .WithAssembly(typeof(Infrastructure.Properties.AssemblyKey).Assembly)
                .WithAssembly(typeof(Model.Properties.AssemblyKey).Assembly)
                .WithAssembly(typeof(Translators.Properties.AssemblyKey).Assembly)
                .WithAssembly(typeof(View.Properties.AssemblyKey).Assembly);

            _container = configuration.CreateContainer();

            IExportProvider exportProvider = new ExportProviderAdapter(_container);
            ExportProvider._instance = exportProvider;

            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(exportProvider));

            exportProvider.GetExportedValues<IService>().ForEach(service => service.Start());

            var tracer = exportProvider.GetExportedValue<ITracer>();
            tracer.WriteLine("Started");
            tracer.WriteLine(ResXManager.Properties.Resources.IntroMessage);
            tracer.WriteLine(ResXManager.Properties.Resources.AssemblyLocation, Path.GetDirectoryName(assembly.Location) ?? "unknown");
            tracer.WriteLine(ResXManager.Properties.Resources.Version, new AssemblyName(assembly.FullName).Version ?? new Version());

            VisualComposition.Error += (_, args) => tracer.TraceError(args.Text);

            MainWindow = exportProvider.GetExportedValue<MainWindow>();
            MainWindow.Show();
        }

        class ExportProvider
        {
            public static IExportProvider? _instance;

            [Export(typeof(IExportProvider))]
            public IExportProvider? Instance => _instance;
        }

        protected override void OnExit([NotNull] ExitEventArgs e)
        {
            Dispose();

            base.OnExit(e);
        }

        public void Dispose()
        {
            _container?.Dispose();
        }
    }
}
