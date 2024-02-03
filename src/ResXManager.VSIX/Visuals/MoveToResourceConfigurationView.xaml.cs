namespace ResXManager.VSIX.Visuals;

using System;
using System.Composition;

using ResXManager.Infrastructure;

using TomsToolbox.Composition;
using TomsToolbox.Wpf.Composition;
using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for MoveToResourceConfigurationView.xaml
/// </summary>
[DataTemplate(typeof(MoveToResourceConfigurationViewModel))]
public partial class MoveToResourceConfigurationView
{
    [ImportingConstructor]
    public MoveToResourceConfigurationView(IExportProvider exportProvider)
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