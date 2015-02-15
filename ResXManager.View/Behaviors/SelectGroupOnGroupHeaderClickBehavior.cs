namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Wpf;

    public class SelectGroupOnGroupHeaderClickBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.MouseLeftButtonDown += GroupHeader_OnMouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.MouseLeftButtonDown -= GroupHeader_OnMouseLeftButtonDown;
        }

        private static void GroupHeader_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Contract.Requires(sender != null);

            var visual = (FrameworkElement)sender;
            var group = visual.DataContext as CollectionViewGroup;
            if ((group == null) || (group.Items == null))
                return;

            var selector = visual.TryFindAncestor<Selector>();

            if (selector == null)
                return;

            var multiSelector = (dynamic)selector;
            selector.BeginInit();

            try
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    multiSelector.SelectedItems.Clear();
                }

                foreach (var item in group.Items)
                {
                    multiSelector.SelectedItems.Add(item);
                }
            }
            catch {} // Element did not have a SelectedItems property.

            selector.EndInit();
        }
    }
}
