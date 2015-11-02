namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition.Hosting;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for CodeReferencesToolTip.xaml
    /// </summary>
    public partial class CodeReferencesToolTip 
    {
        public CodeReferencesToolTip(ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}
