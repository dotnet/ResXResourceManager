namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ResourceTableContextMenu.xaml
    /// </summary>
    [Export]
    public partial class ResourceTableContextMenu
    {
        [ImportingConstructor]
        public ResourceTableContextMenu(ICompositionHost compositionHost)
        {
            this.SetExportProvider(compositionHost.Container);

            InitializeComponent();
        }
    }
}
