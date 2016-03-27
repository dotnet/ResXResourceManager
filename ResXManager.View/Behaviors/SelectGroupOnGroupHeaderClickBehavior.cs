namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Interactivity;

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
            if (group?.Items == null)
                return;

            var selector = visual.TryFindAncestor<Selector>();
            if (selector == null)
                return;

            selector.BeginInit();

            SetSelectedItems((dynamic)selector, group);

            selector.EndInit();
        }

        [ContractVerification(false)] // because of dynamic...
        private static void SetSelectedItems(dynamic multiSelector, CollectionViewGroup group)
        {
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
            catch
            {
            } // Element did not have a SelectedItems property.
        }
    }
}
