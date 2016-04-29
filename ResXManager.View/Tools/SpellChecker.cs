namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    public static class SpellChecker
    {
        private static bool _exceptionTraced;

        [AttachedPropertyBrowsableForType(typeof(TextBoxBase))]
        public static bool GetIsEnabled(TextBoxBase obj)
        {
            Contract.Requires(obj != null);
            return obj.GetValue<bool>(IsEnabledProperty);
        }
        public static void SetIsEnabled(TextBoxBase obj, bool value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="P:tomenglertde.ResXManager.View.Tools.SpellChecker.IsEnabled"/> attached property
        /// </summary>
        /// <AttachedPropertyComments>
        /// <summary>
        /// A exception safe wrapper around <see cref="SpellCheck"/>
        /// </summary>
        /// </AttachedPropertyComments>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SpellChecker), new FrameworkPropertyMetadata(false, IsEnabled_Changed));

        private static void IsEnabled_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = d as TextBoxBase;
            if (textBox == null)
                return;

            try
            {
                SpellCheck.SetIsEnabled(textBox, e.NewValue.SafeCast<bool>());
            }
            catch (Exception ex)
            {
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
}
