namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MoveToResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(MoveToResourceViewModel))]
    public partial class MoveToResourceView
    {
        [ImportingConstructor]
        public MoveToResourceView()
        {
            InitializeComponent();
        }
    }
}
