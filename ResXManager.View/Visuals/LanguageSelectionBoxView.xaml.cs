namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for LanguageSelectionBoxView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageSelectionBoxViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LanguageSelectionBoxView
    {
        [ImportingConstructor]
        public LanguageSelectionBoxView(ExportProvider exportProvider)
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