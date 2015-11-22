namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for CodeGeneratorView.xaml
    /// </summary>
    [VisualCompositionExport(RegionId.ProjectListItemDecorator)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class CodeGeneratorView : IComposablePart
    {
        [ImportingConstructor]
        public CodeGeneratorView(ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}
