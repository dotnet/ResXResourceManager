namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.XamlExtensions;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IDisposable
    {
        [NotNull]
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

            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(folder));

            _compositionHost.AddCatalog(assembly);
            _compositionHost.AddCatalog(new DirectoryCatalog(folder, "*.dll"));

            // ReSharper disable once PossibleNullReferenceException
            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));

            var tracer = _compositionHost.GetExportedValue<ITracer>();
            tracer.WriteLine("Started");

            tracer.WriteLine(ResXManager.Properties.Resources.IntroMessage);
            tracer.WriteLine(ResXManager.Properties.Resources.AssemblyLocation, folder);
            tracer.WriteLine(ResXManager.Properties.Resources.Version, new AssemblyName(assembly.FullName).Version);

            // ReSharper disable once PossibleNullReferenceException
            VisualComposition.Error += (_, args) => tracer.TraceError(args.Text);

            MainWindow = _compositionHost.GetExportedValue<MainWindow>();
            MainWindow.Show();
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
        }
    }
}
