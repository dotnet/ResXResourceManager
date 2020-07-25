namespace ResXManager.View.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;

    using Microsoft.Xaml.Behaviors;

    using Throttle;

    using ResXManager.View.Properties;
    using ResXManager.View.Tools;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    public class SelectAllBehavior : Behavior<ListBox>
    {
        private bool _isListBoxUpdating;
        private PerformanceTracer? _performanceTracer;

        public bool? AreAllFilesSelected
        {
            get => (bool?)GetValue(AreAllFilesSelectedProperty);
            set => SetValue(AreAllFilesSelectedProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="AreAllFilesSelected"/> dependency property
        /// </summary>
        public static readonly DependencyProperty AreAllFilesSelectedProperty =
            DependencyProperty.Register("AreAllFilesSelected", typeof(bool?), typeof(SelectAllBehavior), new FrameworkPropertyMetadata(Settings.Default.AreAllFilesSelected, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) => ((SelectAllBehavior)sender).AreAllFilesSelected_Changed((bool?)e.NewValue)));

        protected override void OnAttached()
        {
            base.OnAttached();

            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            listBox.SelectAll();

            listBox.SelectionChanged += ListBox_SelectionChanged;
            ((INotifyCollectionChanged)listBox.Items).CollectionChanged += (_, __) => ListBox_CollectionChanged();

            _performanceTracer = listBox.GetExportProvider().GetExportedValue<PerformanceTracer>();
        }

        private void ListBox_SelectionChanged(object sender, EventArgs e)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            try
            {
                _isListBoxUpdating = true;

                if (listBox.Items.Count == listBox.SelectedItems.Count)
                {
                    AreAllFilesSelected = true;
                }
                else if (listBox.SelectedItems.Count == 0)
                {
                    AreAllFilesSelected = false;
                }
                else
                {
                    AreAllFilesSelected = null;
                }
            }
            finally
            {
                _isListBoxUpdating = false;
            }
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void ListBox_CollectionChanged()
        {
            var listBox = AssociatedObject;

            if (!AreAllFilesSelected.GetValueOrDefault())
                return;

            _performanceTracer?.Start("ListBox.SelectAll", DispatcherPriority.Input);

            listBox?.SelectAll();
        }

        private void AreAllFilesSelected_Changed(bool? newValue)
        {
            Settings.Default.AreAllFilesSelected = newValue ?? false;

            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            if (_isListBoxUpdating)
                return;

            if (newValue == null)
            {
                Dispatcher?.BeginInvoke(() => AreAllFilesSelected = false);
                return;
            }

            if (newValue.GetValueOrDefault())
            {
                listBox.SelectAll();
            }
            else
            {
                listBox.SelectedIndex = -1;
            }
        }
    }
}
