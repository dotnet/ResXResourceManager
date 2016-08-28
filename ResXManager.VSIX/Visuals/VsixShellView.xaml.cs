namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Media;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for VsixShellView.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class VsixShellView
    {
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public VsixShellView(ExportProvider exportProvider, ThemeManager themeManager)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(themeManager != null);

            try
            {
                this.SetExportProvider(exportProvider);
                _themeManager = themeManager;

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceError(ex.ToString());
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
        private void ObjectInvariant()
        {
            Contract.Invariant(_themeManager != null);
        }
    }
}
