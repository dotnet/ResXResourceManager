namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public class SelectAllBehavior : Behavior<ListBox>
    {
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
            listBox.SelectionChanged += ListBox_Changed;
            ((INotifyCollectionChanged)listBox.Items).CollectionChanged += ListBox_Changed;
        }

        private void ListBox_Changed(object sender, EventArgs e)
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
