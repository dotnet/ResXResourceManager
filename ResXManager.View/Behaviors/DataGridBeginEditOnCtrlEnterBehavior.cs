namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.View.Tools;

    public class DataGridBeginEditOnCtrlEnterBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.PreviewKeyDown += DataGrid_PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.PreviewKeyDown -= DataGrid_PreviewKeyDown;
        }

        private static void DataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Ctrl+Enter on a cell should start editing the cell without clearing the content.

            var dependencyObject = e.OriginalSource as DependencyObject;

            if (dependencyObject.IsChildOfEditingElement())
                return;

            var key = e.Key;
            if ((key != Key.Return) || (!Key.LeftCtrl.IsKeyDown() && !Key.RightCtrl.IsKeyDown()))
                return;

            var dataGrid = (DataGrid)sender;
            Contract.Assume(dataGrid != null);
            dataGrid.BeginEdit();
            e.Handled = true;
        }
    }
}
