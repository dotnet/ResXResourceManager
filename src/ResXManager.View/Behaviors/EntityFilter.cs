namespace ResXManager.View.Behaviors;

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

using ResXManager.Infrastructure;
using ResXManager.Model;

using Throttle;

using TomsToolbox.Essentials;
using TomsToolbox.Wpf;

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
        DependencyProperty.Register(nameof(FilterText), typeof(string), typeof(EntityFilter),
            new FrameworkPropertyMetadata(null, (sender, _) => ((EntityFilter)sender).FilterText_Changed()));

    [Throttled(typeof(Throttle), 300)]
    private void FilterText_Changed()
    {
        var listBox = AssociatedObject;
        if (listBox == null)
            return;

        var value = FilterText;

        listBox.Items.Filter = BuildFilter(value);
    }

    private static Predicate<object>? BuildFilter(string? value)
    {
        value = value?.Trim();

        if (value.IsNullOrEmpty())
            return null;

        try
        {
            var regex = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return item => Filter(item, regex);
        }
        catch (ArgumentException)
        {
        }

        try
        {
            var regex = new Regex(value.Replace(@"\", @"\\", StringComparison.Ordinal), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return item => Filter(item, regex);
        }
        catch (ArgumentException)
        {
        }

        return null;
    }

    private static bool Filter(object item, Regex regex)
    {
        return item is ResourceEntity entity && (regex.IsMatch(entity.DisplayName) || regex.IsMatch(entity.ProjectName));
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        FilterText_Changed();
    }
}
