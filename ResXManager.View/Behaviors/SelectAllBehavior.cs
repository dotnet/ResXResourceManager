namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    using TomsToolbox.Desktop;

    public class SelectAllBehavior : Behavior<ListBox>
    {
        private readonly DispatcherThrottle _collectionChangedThrottle;

        public SelectAllBehavior()
        {
            _collectionChangedThrottle = new DispatcherThrottle(ListBox_CollectionChanged);
        }

        public bool? AreAllFilesSelected
        {
            get { return (bool?)GetValue(AreAllFilesSelectedProperty); }
            set { SetValue(AreAllFilesSelectedProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="AreAllFilesSelected"/> dependency property
        /// </summary>
        public static readonly DependencyProperty AreAllFilesSelectedProperty =
            DependencyProperty.Register("AreAllFilesSelected", typeof(bool?), typeof(SelectAllBehavior), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) => ((SelectAllBehavior)sender).AreAllFilesSelected_Changed((bool?)e.NewValue)));

        protected override void OnAttached()
        {
            base.OnAttached();

            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            listBox.SelectAll();

            listBox.SelectionChanged += ListBox_SelectionChanged;
            ((INotifyCollectionChanged)listBox.Items).CollectionChanged += (_, __) => _collectionChangedThrottle.Tick();
        }

        private void ListBox_SelectionChanged(object sender, EventArgs e)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

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

        private void ListBox_CollectionChanged()
        {
            var listBox = AssociatedObject;

            if (AreAllFilesSelected.GetValueOrDefault())
            {
                listBox?.SelectAll();
            }
        }

        private void AreAllFilesSelected_Changed(bool? newValue)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            if (!newValue.HasValue)
                return;

            if (newValue.Value)
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
