namespace ResXManager.View.Visuals;

using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for WebExportConfigurationView.xaml
/// </summary>
[DataTemplate(typeof(WebExportConfigurationViewModel))]
public partial class WebExportConfigurationView
{
    public WebExportConfigurationView()
    {
        InitializeComponent();
    }
}
