namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for LanguageSelectionBoxView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageSelectionBoxViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LanguageSelectionBoxView
    {
        [ImportingConstructor]
        public LanguageSelectionBoxView(ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}