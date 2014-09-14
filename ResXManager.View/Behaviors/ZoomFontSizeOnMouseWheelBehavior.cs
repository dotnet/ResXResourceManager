namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class ZoomFontSizeOnMouseWheelBehavior : Behavior<FrameworkElement>
    {
        private double _initialFontSize;
        private double _zoom = 1.0;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            _initialFontSize = TextElement.GetFontSize(AssociatedObject);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

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

            TextElement.SetFontSize(AssociatedObject, _initialFontSize * _zoom);
        }
    }
}
