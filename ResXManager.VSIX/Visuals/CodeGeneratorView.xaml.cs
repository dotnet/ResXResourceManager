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
    /// Interaction logic for CodeGeneratorView.xaml
    /// </summary>
    [VisualCompositionExport(RegionId.ProjectListItemDecorator)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class CodeGeneratorView
    {
        [ImportingConstructor]
        public CodeGeneratorView([NotNull] IExportProvider exportProvider)
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
