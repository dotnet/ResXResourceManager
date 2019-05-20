namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    public static class Spellcheck
    {
        private static bool _exceptionTraced;

        [AttachedPropertyBrowsableForType(typeof(TextBoxBase))]
        public static bool GetIsEnabled([NotNull] TextBoxBase obj)
        {
            return obj.GetValue<bool>(IsEnabledProperty);
        }
        public static void SetIsEnabled([NotNull] TextBoxBase obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="P:tomenglertde.ResXManager.View.Tools.Spellcheck.IsEnabled"/> attached property
        /// </summary>
        /// <AttachedPropertyComments>
        /// <summary>
        /// A exception safe wrapper around <see cref="System.Windows.Controls.SpellCheck"/>
        /// </summary>
        /// </AttachedPropertyComments>
        [NotNull]
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(Spellcheck), new FrameworkPropertyMetadata(false, IsEnabled_Changed));

        private static void IsEnabled_Changed([CanBeNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
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
}
