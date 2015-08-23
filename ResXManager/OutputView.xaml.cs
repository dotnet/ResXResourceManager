namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    [DataTemplate(typeof(OutputViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class OutputView
    {
        public OutputView()
        {
            InitializeComponent();
        }
    }
}
