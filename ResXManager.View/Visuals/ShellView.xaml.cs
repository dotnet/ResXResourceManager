namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [Export]
    [DataTemplate(typeof(ShellViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ShellView : IComposablePart
    {
        [ImportingConstructor]
        public ShellView(ICompositionHost compositionHost)
        {
            Contract.Requires(compositionHost != null);

            this.SetExportProvider(compositionHost.Container);

            References.Resolve(this);

            InitializeComponent();
        }
    }
}
