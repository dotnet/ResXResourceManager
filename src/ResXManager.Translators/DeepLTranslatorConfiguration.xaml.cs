namespace ResXManager.Translators;

using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for DeepLTranslatorConfiguration.xaml
/// </summary>
[DataTemplate(typeof(DeepLTranslator))]
public partial class DeepLTranslatorConfiguration
{
    public DeepLTranslatorConfiguration()
    {
        InitializeComponent();
    }
}
