namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.Mef;

    /// <summary>
    /// Interaction logic for LanguageSelectionBoxView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageSelectionBoxViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LanguageSelectionBoxView
    {
        [ImportingConstructor]
        public LanguageSelectionBoxView([NotNull] IExportProvider exportProvider)
        {
            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }
    }
}