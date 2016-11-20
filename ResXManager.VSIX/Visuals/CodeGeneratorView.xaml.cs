namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for CodeGeneratorView.xaml
    /// </summary>
    [VisualCompositionExport(RegionId.ProjectListItemDecorator)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class CodeGeneratorView
    {
        [ImportingConstructor]
        public CodeGeneratorView([NotNull] ExportProvider exportProvider)
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
