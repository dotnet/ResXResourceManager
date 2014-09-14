namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.View.Tools;

    public class PopupFocusManagerBehavior : Behavior<Popup>
    {
        /// <summary>
        /// Gets or sets the toggle button that controls the popup.
        /// </summary>
        public ToggleButton ToggleButton
        {
            get { return (ToggleButton)GetValue(ToggleButtonProperty); }
            set { SetValue(ToggleButtonProperty, value); }
        }
        /// <summary>
        /// Identifies the ToggleButton dependency property
        /// </summary>
        public static readonly DependencyProperty ToggleButtonProperty =
            DependencyProperty.Register("ToggleButton", typeof (ToggleButton), typeof (PopupFocusManagerBehavior));

        private Popup Popup
        {
            get
            {
                return AssociatedObject;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            Popup.IsKeyboardFocusWithinChanged += Popup_IsKeyboardFocusWithinChanged;
            Popup.Opened += Popup_Opened;
            Popup.KeyDown += Popup_KeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Popup.IsKeyboardFocusWithinChanged -= Popup_IsKeyboardFocusWithinChanged;
            Popup.Opened -= Popup_Opened;
            Popup.KeyDown -= Popup_KeyDown;
        }

        void Popup_KeyDown(object sender, KeyEventArgs e)
        {
            if (ToggleButton == null)
                return;

            switch (e.Key)
            {
                case Key.Escape:
                case Key.Enter:
                case Key.Tab:
                    ToggleButton.Focus();
                    break;
            }
        }

        void Popup_Opened(object sender, System.EventArgs e)
        {
            var popup = (Popup)sender;
            var focusable = popup.Child.VisualDescendantsAndSelf<UIElement>().FirstOrDefault(item => item.Focusable);
            if (focusable != null)
            {
                Dispatcher.BeginInvoke(new Action(() => focusable.Focus()));
            }
        }

        void Popup_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue.Equals(false) && (ToggleButton != null))
            {
                ToggleButton.IsChecked = false;
            }
        }



    }
}
