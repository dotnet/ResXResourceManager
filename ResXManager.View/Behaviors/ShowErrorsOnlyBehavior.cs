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

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    using TomsToolbox.Wpf;

    public class ShowErrorsOnlyBehavior : Behavior<DataGrid>
    {
        [CanBeNull]
        public ToggleButton ToggleButton
        {
            get { return (ToggleButton)GetValue(ToggleButtonProperty); }
            set { SetValue(ToggleButtonProperty, value); }
        }
        /// <summary>
        /// Identifies the ToggleButton dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty ToggleButtonProperty =
            DependencyProperty.Register("ToggleButton", typeof(ToggleButton), typeof(ShowErrorsOnlyBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((ShowErrorsOnlyBehavior)sender).ToggleButton_Changed((ToggleButton)e.OldValue, (ToggleButton)e.NewValue)));

        public void Refresh()
        {
            Refresh(ToggleButton);
        }

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

        [CanBeNull]
        private DataGrid DataGrid
        {
            get
            {
                Contract.Ensures((AssociatedObject == null) || (Contract.Result<DataGrid>() != null));
                return AssociatedObject;
            }
        }

        private void ToggleButton_Changed([CanBeNull] ToggleButton oldValue, [CanBeNull] ToggleButton newValue)
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

        private void ToggleButton_StateChanged([NotNull] object sender, [NotNull] EventArgs e)
        {
            Refresh((ToggleButton)sender);
        }

        private void Refresh([CanBeNull] ToggleButton button)
        {
            var dataGrid = DataGrid;

            if ((button == null) || (dataGrid == null))
                return;

            UpdateErrorsOnlyFilter(button.IsChecked.GetValueOrDefault());

            var selectedItem = dataGrid.SelectedItem;
            if (selectedItem != null)
                dataGrid.ScrollIntoView(selectedItem);
        }

        private void DataGrid_ColumnVisibilityChanged([NotNull] object source, [NotNull] EventArgs e)
        {
            var toggleButton = ToggleButton;

            if (toggleButton == null)
                return;

            if (toggleButton.IsChecked.GetValueOrDefault())
            {
                toggleButton.BeginInvoke(() => UpdateErrorsOnlyFilter(true));
            }
        }

        private void UpdateErrorsOnlyFilter(bool isEnabled)
        {
            var dataGrid = DataGrid;

            if (dataGrid == null)
                return;

            try
            {
                dataGrid.CommitEdit();

                if (!isEnabled)
                {
                    dataGrid.Items.Filter = null;
                    dataGrid.SetIsAutoFilterEnabled(true);
                    return;
                }

                var visibleLanguages = dataGrid.Columns
                    .Where(column => column.Visibility == Visibility.Visible)
                    .Select(column => column.Header)
                    .OfType<LanguageHeader>()
                    .Select(header => header.CultureKey)
                    .ToArray();

                dataGrid.SetIsAutoFilterEnabled(false);

                dataGrid.Items.Filter = row =>
                {
                    var entry = (ResourceTableEntry)row;
                    Contract.Assume(entry != null);

                    var neutralCulture = entry.NeutralLanguage.CultureKey;

                    var hasInvariantMismatches = visibleLanguages
                        .Where(lang => neutralCulture != lang)
                        .Select(lang => new { Value = entry.Values.GetValue(lang), IsInvariant = entry.IsItemInvariant.GetValue(lang) || entry.IsInvariant })
                        .Any(v => v.IsInvariant != string.IsNullOrEmpty(v.Value));

                    return entry.IsDuplicateKey
                        || hasInvariantMismatches
                        || entry.HasStringFormatParameterMismatches(visibleLanguages)
                        || entry.HasSnapshotDifferences(visibleLanguages);
                };
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
