namespace ResXManager.View.Behaviors
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.Infrastructure;
    using TomsToolbox.Essentials;

    public class EntityFilter : Behavior<ListBox>
    {
        public string? FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="FilterText"/> dependency property
        /// </summary>
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(EntityFilter),
                new FrameworkPropertyMetadata(null, (sender, e) => ((EntityFilter)sender).FilterText_Changed((string)e.NewValue)));

        private void FilterText_Changed(string? value)
        {
            var listBox = AssociatedObject;
            if (listBox == null)
                return;

            listBox.Items.Filter = BuildFilter(value);
        }

        public static Predicate<object>? BuildFilter(string? value)
        {
            value = value?.Trim();

            if (value.IsNullOrEmpty())
                return null;

            try
            {
                var regex = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return item => regex.Match(item.ToString() ?? string.Empty).Success;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                var regex = new Regex(value.Replace(@"\", @"\\", StringComparison.Ordinal), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return item => regex.Match(item.ToString() ?? string.Empty).Success;
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
