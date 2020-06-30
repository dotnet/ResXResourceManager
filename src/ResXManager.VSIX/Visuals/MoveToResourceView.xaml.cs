namespace ResXManager.VSIX.Visuals
{
    using System;
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for MoveToResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(MoveToResourceViewModel))]
    public partial class MoveToResourceView
    {
        [ImportingConstructor]
        public MoveToResourceView([NotNull] IExportProvider exportProvider)
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
