namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.Mef;

    /// <summary>
    /// Interaction logic for MoveToResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(MoveToResourceViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
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
