namespace ResXManager.View.Behaviors
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;

    using TomsToolbox.Wpf;

    public class ShowErrorsOnlyBehavior : Behavior<DataGrid>
    {
        [CanBeNull]
        public ToggleButton ToggleButton
        {
            get => (ToggleButton)GetValue(ToggleButtonProperty);
            set => SetValue(ToggleButtonProperty, value);
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

            DataGrid.GetAdditionalEvents().ColumnVisibilityChanged += DataGrid_ColumnVisibilityChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            DataGrid.GetAdditionalEvents().ColumnVisibilityChanged -= DataGrid_ColumnVisibilityChanged;
        }

        [NotNull]
        private DataGrid DataGrid => AssociatedObject;

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

            if ((button == null) || (AssociatedObject == null))
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
            if (AssociatedObject == null)
                return;

            var dataGrid = DataGrid;

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
                    var neutralCulture = entry.NeutralLanguage.CultureKey;

                    var hasInvariantMismatches = visibleLanguages
                        .Select(lang => new
                        {
                            IsNeutral = lang == neutralCulture,
                            IsEmpty = string.IsNullOrEmpty(entry.Values.GetValue(lang)),
                            IsInvariant = entry.IsItemInvariant.GetValue(lang) || entry.IsInvariant
                        })
                        .Any(item => item.IsNeutral ? !item.IsInvariant && item.IsEmpty : item.IsInvariant != item.IsEmpty);

                    return entry.IsDuplicateKey
                        || hasInvariantMismatches
                        || entry.HasRulesMismatches(visibleLanguages)
                        || entry.HasSnapshotDifferences(visibleLanguages);
                };
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
