namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.Mef;

    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    [DataTemplate(typeof(OutputViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class OutputView
    {
        [ImportingConstructor]
        public OutputView([NotNull] IExportProvider exportProvider)
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
