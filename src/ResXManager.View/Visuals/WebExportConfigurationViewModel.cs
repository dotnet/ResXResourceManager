namespace ResXManager.View.Visuals
{
    using System.ComponentModel;
    using System.Windows;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Properties;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [LocalizedDisplayName(StringResourceKey.WebProjectFileExport_Title)]
    [VisualCompositionExport(RegionId.Configuration)]
    internal sealed class WebExportConfigurationViewModel
    {
        public WebExportConfigurationViewModel(ResourceManager resourceManager)
        {
            SolutionFolder = resourceManager.SolutionFolder ?? string.Empty;

            if (!WebFileExporterConfiguration.Load(SolutionFolder, out var configuration))
            {
                configuration = new WebFileExporterConfiguration();
            }

            Configuration = configuration;
            configuration.PropertyChanged += Configuration_PropertyChanged;
        }

        public WebFileExporterConfiguration Configuration { get; }

        public string? SolutionFolder { get; }

        public static void SetIsConfigurationEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsConfigurationEnabledProperty, value);
        }
        public static bool GetIsConfigurationEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsConfigurationEnabledProperty);
        }
        public static readonly DependencyProperty IsConfigurationEnabledProperty = DependencyProperty.RegisterAttached(
            "IsConfigurationEnabled", typeof(bool), typeof(WebExportConfigurationViewModel), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveChanges();
        }

        [Throttled(typeof(Throttle), 500)]
        private void SaveChanges()
        {
            if (!SolutionFolder.IsNullOrEmpty())
            {
                Configuration.Save(SolutionFolder);
            }
        }
    }
}
