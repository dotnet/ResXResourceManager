namespace ResXManager.View.Visuals;

using System;

using ResXManager.Infrastructure;

using TomsToolbox.Composition;
using TomsToolbox.Wpf.Composition;

/// <summary>
/// Interaction logic for CodeReferencesToolTip.xaml
/// </summary>
public partial class CodeReferencesToolTip
{
    public CodeReferencesToolTip(IExportProvider exportProvider)
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