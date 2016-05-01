namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public class EntityFilter : Behavior<ListBox>
    {
        public string FilterText
        {
            get { return (string)GetValue(FilterTextProperty); }
            set { SetValue(FilterTextProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="FilterText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(EntityFilter),
                new FrameworkPropertyMetadata(null, (sender, e) => ((EntityFilter)sender).FilterText_Changed((string)e.NewValue)));

        private void FilterText_Changed(string value)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var regex = new Regex(value.Trim(), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    listBox.Items.Filter = item => regex.Match(item.ToString()).Success;
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            listBox.Items.Filter = null;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            FilterText_Changed(FilterText);
        }
    }
}
