namespace ResXManager.Translators;

using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for AzureTranslatorConfiguration.xaml
/// </summary>
[DataTemplate(typeof(AzureTranslator))]
public partial class AzureTranslatorConfiguration
{
    public AzureTranslatorConfiguration()
    {
        InitializeComponent();
    }
}