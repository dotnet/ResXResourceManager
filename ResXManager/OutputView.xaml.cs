namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    [DataTemplate(typeof(OutputViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class OutputView
    {
        [ImportingConstructor]
        public OutputView(ExportProvider exportProvider)
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
