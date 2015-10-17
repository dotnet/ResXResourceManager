namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interactivity;

    using DataGridExtensions;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    using TomsToolbox.Wpf;

    public class ShowErrorsOnlyBehavior : Behavior<DataGrid>
    {
        public ToggleButton ToggleButton
        {
            get { return (ToggleButton)GetValue(ToggleButtonProperty); }
            set { SetValue(ToggleButtonProperty, value); }
        }
        /// <summary>
        /// Identifies the ToggleButton dependency property
        /// </summary>
        public static readonly DependencyProperty ToggleButtonProperty =
            DependencyProperty.Register("ToggleButton", typeof(ToggleButton), typeof(ShowErrorsOnlyBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((ShowErrorsOnlyBehavior)sender).ToggleButton_Changed((ToggleButton)e.OldValue, (ToggleButton)e.NewValue)));

        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            DataGrid.GetAdditionalEvents().ColumnVisibilityChanged += DataGrid_ColumnVisibilityChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            DataGrid.GetAdditionalEvents().ColumnVisibilityChanged -= DataGrid_ColumnVisibilityChanged;
        }

        private DataGrid DataGrid
        {
            get
            {
                Contract.Ensures((AssociatedObject == null) || (Contract.Result<DataGrid>() != null));
                return AssociatedObject;
            }
        }

        private void ToggleButton_Changed(ToggleButton oldValue, ToggleButton newValue)
        {
            if (oldValue != null)
            {
                oldValue.Checked -= ToggleButton_StateChanged;
                oldValue.Unchecked -= ToggleButton_StateChanged;
            }

            if (newValue != null)
            {
                newValue.Checked += ToggleButton_StateChanged;
                newValue.Unchecked += ToggleButton_StateChanged;
                ToggleButton_StateChanged(newValue, EventArgs.Empty);
            }
        }

        private void ToggleButton_StateChanged(object sender, EventArgs e)
        {
            if ((sender == null) || (DataGrid == null))
                return;

            var button = (ToggleButton)sender;

            if (button.IsChecked.GetValueOrDefault())
            {
                UpdateErrorsOnlyFilter();
            }
            else
            {
                DataGrid.Items.Filter = null;
                DataGrid.SetIsAutoFilterEnabled(true);
            }

            var selectedItem = DataGrid.SelectedItem;
            if (selectedItem != null)
                DataGrid.ScrollIntoView(selectedItem);
        }

        private void DataGrid_ColumnVisibilityChanged(object source, EventArgs e)
        {
            var toggleButton = ToggleButton;

            if (toggleButton == null)
                return;

            if (toggleButton.IsChecked.GetValueOrDefault())
            {
                toggleButton.BeginInvoke(UpdateErrorsOnlyFilter);
            }
        }

        private void UpdateErrorsOnlyFilter()
        {
            if (DataGrid == null)
                return;

            var visibleLanguages = DataGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible)
                .Select(column => column.Header)
                .OfType<LanguageHeader>()
                .Select(header => header.CultureKey)
                .ToArray();

            DataGrid.SetIsAutoFilterEnabled(false);
            DataGrid.Items.Filter = row =>
            {
                var entry = (ResourceTableEntry)row;
                var values = visibleLanguages.Select(lang => entry.Values.GetValue(lang));

                return entry.IsDuplicateKey || (!entry.IsInvariant && (values.Any(string.IsNullOrEmpty) || entry.HasStringFormatParameterMismatches(visibleLanguages)));
            };
        }
    }
}
