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
    /// Interaction logic for MoveToResourceConfigurationView.xaml
    /// </summary>
    [DataTemplate(typeof(MoveToResourceConfigurationViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MoveToResourceConfigurationView
    {
        [ImportingConstructor]
        public MoveToResourceConfigurationView([NotNull] IExportProvider exportProvider)
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
