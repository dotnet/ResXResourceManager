namespace ResXManager
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using Ninject;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Ninject;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.XamlExtensions;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IDisposable
    {
        private readonly IKernel _kernel = new StandardKernel();

        public App()
        {
#if DEBUG && TEST_L10N
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
#endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var assembly = GetType().Assembly;

            _kernel.BindExports(assembly,
                typeof(Infrastructure.Properties.AssemblyKey).Assembly,
                typeof(Model.Properties.AssemblyKey).Assembly,
                typeof(Translators.Properties.AssemblyKey).Assembly,
                typeof(View.Properties.AssemblyKey).Assembly);

            IExportProvider exportProvider = new ExportProvider(_kernel);
            _kernel.Bind<IExportProvider>().ToConstant(exportProvider);

            Resources.MergedDictionaries.Add(TomsToolbox.Wpf.Styles.WpfStyles.GetDefaultStyles());
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

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();

            base.OnExit(e);
        }

        public void Dispose()
        {
            _kernel?.Dispose();
        }
    }
}
