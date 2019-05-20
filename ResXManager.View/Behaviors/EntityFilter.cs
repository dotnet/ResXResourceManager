namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    using JetBrains.Annotations;

    public class EntityFilter : Behavior<ListBox>
    {
        [CanBeNull]
        public string FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="FilterText"/> dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(EntityFilter),
                new FrameworkPropertyMetadata(null, (sender, e) => ((EntityFilter)sender).FilterText_Changed((string)e.NewValue)));

        private void FilterText_Changed([CanBeNull] string value)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            listBox.Items.Filter = BuildFilter(value);
        }

        [CanBeNull]
        public static Predicate<object> BuildFilter([CanBeNull] string value)
        {
            value = value?.Trim();

            if (string.IsNullOrEmpty(value)) 
                return null;

            try
            {
                var regex = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return item => regex.Match(item.ToString()).Success;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                var regex = new Regex(value.Replace(@"\", @"\\"), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return item => regex.Match(item.ToString()).Success;
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            FilterText_Changed(FilterText);
        }
    }
}
