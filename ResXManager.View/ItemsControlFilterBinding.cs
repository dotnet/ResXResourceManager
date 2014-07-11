namespace tomenglertde.ResXManager.View
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;

    public static class ItemsControlFilterBinding
    {
        public static string GetRegexFilter(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (string)obj.GetValue(RegexFilterProperty);
        }
        public static void SetRegexFilter(DependencyObject obj, string value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(RegexFilterProperty, value);
        }
        public static readonly DependencyProperty RegexFilterProperty =
            DependencyProperty.RegisterAttached("RegexFilter", typeof(string), typeof(ItemsControlFilterBinding), new FrameworkPropertyMetadata(RegexFilter_Changed));

        private static void RegexFilter_Changed(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = source as ItemsControl;

            if (itemsControl == null)
                return;

            var items = itemsControl.Items;

            var filterText = e.NewValue as string;

            if (!string.IsNullOrEmpty(filterText))
            {
                try
                {
                    var regex = new Regex(filterText, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    items.Filter = item => regex.Match(item.ToString()).Success;
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            items.Filter = null;
        }
    }
}
