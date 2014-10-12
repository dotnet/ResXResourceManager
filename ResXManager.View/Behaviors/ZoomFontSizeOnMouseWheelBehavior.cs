namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class ZoomFontSizeOnMouseWheelBehavior : Behavior<FrameworkElement>
    {
        private double? _initialFontSize;
        private double _zoom = 1.0;

        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if ((!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) || (e.Delta == 0))
                return;

            e.Handled = true;

            if (e.Delta > 0)
            {
                _zoom *= 1.1;
            }
            else
            {
                _zoom /= 1.1;
            }

            _zoom = Math.Max(0.5, Math.Min(5, _zoom));

            if (!_initialFontSize.HasValue)
            {
                _initialFontSize = TextElement.GetFontSize(AssociatedObject);
            }

            var effectiveZoom = Math.Round(_zoom, 1);

            TextElement.SetFontSize(AssociatedObject, _initialFontSize.Value * effectiveZoom);

            if (Math.Abs(1.0 - effectiveZoom) < 0.1)
            {
                _initialFontSize = null;
            }
        }
    }
}
