namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    [DataTemplate(typeof(MainViewModel))] 
    [PartCreationPolicy(CreationPolicy.NonShared)]
    
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }
    }
}
