namespace ResXManager
{
    using System;
    using System.Composition;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    [DataTemplate(typeof(OutputViewModel))]
    public partial class OutputView
    {
        [ImportingConstructor]
        public OutputView(IExportProvider exportProvider)
        {
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
