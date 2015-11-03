
namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ProjectListContextMenu.xaml
    /// </summary>
    [Export]
    public partial class ProjectListContextMenu
    {
        [ImportingConstructor]
        public ProjectListContextMenu(ICompositionHost compositionHost)
        {
            this.SetExportProvider(compositionHost.Container);

            InitializeComponent();
        }
    }
}
