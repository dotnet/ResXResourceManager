namespace ResXManager.Translators
{
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for AzureOpenAITranslatorConfiguration.xaml
    /// </summary>
    [DataTemplate(typeof(AzureOpenAITranslator))]
    public partial class AzureOpenAITranslatorConfiguration
    {
        public AzureOpenAITranslatorConfiguration()
        {
            InitializeComponent();
        }
    }
}