namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for VsixShellView.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class VsixShellView
    {
        [ImportingConstructor]
        public VsixShellView(ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }
    }
}
