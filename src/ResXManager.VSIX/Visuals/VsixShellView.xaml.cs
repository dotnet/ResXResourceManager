namespace ResXManager.VSIX.Visuals
{
    using System;
    using System.Composition;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.View.Themes;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for VsixShellView.xaml
    /// </summary>
    [Export]
    public partial class VsixShellView
    {
        [NotNull]
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public VsixShellView([NotNull] IExportProvider exportProvider, [NotNull] ThemeManager themeManager, VsixShellViewModel viewModel)
        {
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
                exportProvider.TraceXamlLoaderError(ex);
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

        private void Self_Loaded(object sender, RoutedEventArgs e)
        {
            this.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                if (Content != null)
                    return;

                var exportProvider = this.GetExportProvider();

                exportProvider.TraceXamlLoaderError(null);
            });
        }
    }
}
