namespace ResXManager.View.Visuals;

using System;
using System.Composition;
using System.Windows;
using System.Windows.Controls;

using ResXManager.Infrastructure;

using TomsToolbox.Composition;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition;
using TomsToolbox.Wpf.Composition.AttributedModel;

/// <summary>
/// Interaction logic for Translations.xaml
/// </summary>
[DataTemplate(typeof(TranslationsViewModel))]
[Shared]
public partial class TranslationsView
{
    [ImportingConstructor]
    public TranslationsView(IExportProvider exportProvider)
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

    private void ComboBox_IsKeyboardFocusWithinChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (!true.Equals(e.NewValue))
            return;

        var element = sender as DependencyObject;

        var row = element?.TryFindAncestor<DataGridRow>();
        if (row != null)
        {
            row.IsSelected = true;
        }
    }
}