namespace ResXManager.VSIX.Visuals;

using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for ShowErrorsConfigurationView.xaml
/// </summary>
[DataTemplate(typeof(ShowErrorsConfigurationViewModel))]
public partial class ShowErrorsConfigurationView
{
    public ShowErrorsConfigurationView()
    {
        InitializeComponent();
    }
}
