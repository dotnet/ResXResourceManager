namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    [DataTemplate(typeof(MainViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainView
    {
        [ImportingConstructor]
        public MainView(ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceError(ex.ToString());
            }
        }
    }
}
