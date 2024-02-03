namespace ResXManager.View.Tools;

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition;

public static class Spellcheck
{
    private static bool _exceptionTraced;

    [AttachedPropertyBrowsableForType(typeof(TextBoxBase))]
    public static bool GetIsEnabled(TextBoxBase item)
    {
        return item.GetValue<bool>(IsEnabledProperty);
    }
    public static void SetIsEnabled(TextBoxBase item, bool value)
    {
        item.SetValue(IsEnabledProperty, value);
    }
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Identifies the <see cref="P:ResXManager.View.Tools.Spellcheck.IsEnabled"/> attached property
    /// </summary>
    /// <AttachedPropertyComments>
    /// <summary>
    /// A exception safe wrapper around <see cref="System.Windows.Controls.SpellCheck"/>
    /// </summary>
    /// </AttachedPropertyComments>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(Spellcheck), new FrameworkPropertyMetadata(false, IsEnabled_Changed));

    private static void IsEnabled_Changed(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = d as TextBoxBase;
        if (textBox == null)
            return;

        try
        {
            textBox.SpellCheck.IsEnabled = e.NewValue.SafeCast<bool>();
        }
        catch (Exception ex)
        {
            textBox.SpellCheck.IsEnabled = false;

            if (_exceptionTraced)
                return;

            var message = ex.Message;

            textBox.BeginInvoke(DispatcherPriority.Background, () =>
            {
                var exportProvider = textBox.TryGetExportProvider();
                var tracer = exportProvider?.GetExportedValueOrDefault<ITracer>();
                tracer?.TraceError("Failed to enable the spell checker: " + message);
            });

            _exceptionTraced = true;
        }
    }
}