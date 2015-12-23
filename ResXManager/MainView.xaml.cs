namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    [DataTemplate(typeof(MainViewModel))] 
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainView
    {
        [ImportingConstructor]
        public MainView(ICompositionHost compositionHost)
        {
            Contract.Requires(compositionHost != null);

            this.SetExportProvider(compositionHost.Container);

            InitializeComponent();
        }
    }
}
