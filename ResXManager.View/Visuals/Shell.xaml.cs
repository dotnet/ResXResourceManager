namespace tomenglertde.ResXManager.View.Visuals
{
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    [VisualCompositionExport("Shell")]
    public partial class Shell : IComposablePart
    {
        public Shell()
        {
            References.Resolve(this);

            InitializeComponent();
        }
    }
}
