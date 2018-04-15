namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for VsixShellView.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class VsixShellView
    {
        [NotNull]
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public VsixShellView([NotNull] ExportProvider exportProvider, [NotNull] ThemeManager themeManager, VsixShellViewModel viewModel)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(themeManager != null);

            _themeManager = themeManager;

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();

                DataContext = viewModel;
                Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(exportProvider));
            }
            catch (Exception ex)
            {
                exportProvider.TraceError(ex.ToString());

                var path = Path.GetDirectoryName(GetType().Assembly.Location);
                Contract.Assume(!string.IsNullOrEmpty(path));

                var assemblyFileNames = Directory.EnumerateFiles(path, @"*.dll")
                    .Where(file => !"DocumentFormat.OpenXml.dll".Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var assemblyNames = new HashSet<string>(assemblyFileNames.Select(Path.GetFileNameWithoutExtension));

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                var messages = loadedAssemblies
                    .Where(a => assemblyNames.Contains(a.GetName().Name))
                    .Select(assembly => string.Format(CultureInfo.CurrentCulture, "Assembly '{0}' loaded from {1}", assembly.FullName, assembly.CodeBase))
                    .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var message in messages)
                {
                    exportProvider.WriteLine(message);
                }
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if ((e.Property != ForegroundProperty) && (e.Property != BackgroundProperty))
                return;

            var foreground = ToGray((Foreground as SolidColorBrush)?.Color);
            var background = ToGray((Background as SolidColorBrush)?.Color);

            _themeManager.IsDarkTheme = background < foreground;
        }

        private static double ToGray(Color? color)
        {
            return color?.R * 0.21 + color?.G * 0.72 + color?.B * 0.07 ?? 0.0;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_themeManager != null);
        }
    }
}
