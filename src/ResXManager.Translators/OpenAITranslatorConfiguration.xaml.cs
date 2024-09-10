namespace ResXManager.Translators;

using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for OpenAITranslatorConfiguration.xaml
/// </summary>
[DataTemplate(typeof(OpenAITranslator))]
public partial class OpenAITranslatorConfiguration
{
    public OpenAITranslatorConfiguration()
    {
        InitializeComponent();
    }
}