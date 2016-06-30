namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MoveToResourceConfigurationView.xaml
    /// </summary>
    [DataTemplate(typeof(MoveToResourceConfigurationViewModel))]
    public partial class MoveToResourceConfigurationView
    {
        [ImportingConstructor]
        public MoveToResourceConfigurationView()
        {
            InitializeComponent();
        }
    }
}
