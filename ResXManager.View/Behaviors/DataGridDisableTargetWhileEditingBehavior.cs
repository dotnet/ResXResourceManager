namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public class DataGridDisableTargetWhileEditingBehavior : Behavior<DataGrid>
    {
        public UIElement Target
        {
            get { return (UIElement)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }
        /// <summary>
        /// Identifies the Target dependency property
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof (UIElement), typeof (DataGridDisableTargetWhileEditingBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            
            AssociatedObject.PreparingCellForEdit += DataGrid_PreparingCellForEdit;
            AssociatedObject.CellEditEnding += DataGrid_CellEditEnding;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreparingCellForEdit -= DataGrid_PreparingCellForEdit;
            AssociatedObject.CellEditEnding -= DataGrid_CellEditEnding;
        }

        void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (Target == null)
                return;

            Target.IsEnabled = true;
        }

        void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (Target == null)
                return;

            Target.IsEnabled = false;
        }
    }
}
