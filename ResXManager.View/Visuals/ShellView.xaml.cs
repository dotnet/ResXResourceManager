namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [DataTemplate(typeof(ShellViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ShellView : IComposablePart
    {
        public ShellView()
        {
            References.Resolve(this);

            InitializeComponent();
        }
    }
}
