namespace ResXManager.VSIX.Visuals
{
    using System;
    using System.Composition;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;

    using ResXManager.Infrastructure;
    using ResXManager.View;
    using ResXManager.View.Themes;
    using ResXManager.VSIX.Compatibility;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for VsixShellView.xaml
    /// </summary>
    [Export]
    public partial class VsixShellView
    {
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public VsixShellView(IExportProvider exportProvider, ThemeManager themeManager, IVsixShellViewModel viewModel)
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

            var foreground = ((Foreground as SolidColorBrush)?.Color).ToGray();
            var background = ((Background as SolidColorBrush)?.Color).ToGray();

            _themeManager.IsDarkTheme = background < foreground;
        }

        private void Self_Loaded(object? sender, RoutedEventArgs e)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            this.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                if (Content != null)
                    return;

                var exportProvider = this.GetExportProvider();

                exportProvider.TraceXamlLoaderError(null);
            });
#pragma warning restore VSTHRD110 // Observe result of async calls
        }
    }
}
