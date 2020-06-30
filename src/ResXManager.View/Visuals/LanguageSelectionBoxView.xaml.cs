namespace ResXManager.View.Visuals
{
    using System;
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for LanguageSelectionBoxView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageSelectionBoxViewModel))]
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