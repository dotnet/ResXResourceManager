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
            new FrameworkPropertyMetadata(null, (sender, _) => ((EntityFilter)sender).Filter_Changed()));

    public string? ProjectFilter
    {
        get => (string)GetValue(ProjectFilterProperty);
        set => SetValue(ProjectFilterProperty, value);
    }
    /// <summary>
    /// Identifies the <see cref="ProjectFilter"/> dependency property
    /// </summary>
    public static readonly DependencyProperty ProjectFilterProperty =
        DependencyProperty.Register(nameof(ProjectFilter), typeof(string), typeof(EntityFilter),
            new FrameworkPropertyMetadata(null, (sender, _) => ((EntityFilter)sender).Filter_Changed()));

    [Throttled(typeof(Throttle), 300)]
    private void Filter_Changed()
    {
        var listBox = AssociatedObject;
        if (listBox == null)
            return;

        var textFilter = BuildTextFilter(FilterText);
        var projectFilter = ProjectFilter;

        if (textFilter == null && projectFilter == null)
        {
            listBox.Items.Filter = null;
            return;
        }

        listBox.Items.Filter = item =>
        {
            if (projectFilter != null && item is ResourceEntity entity && !string.Equals(entity.ProjectName, projectFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (textFilter != null && !textFilter(item))
                return false;

            return true;
        };
    }

    public static Predicate<object>? BuildTextFilter(string? value)
    {
        value = value?.Trim();

        if (value.IsNullOrEmpty())
            return null;

        try
        {
            var regex = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return item => regex.IsMatch(item?.ToString() ?? string.Empty);
        }
        catch (ArgumentException)
        {
        }

        try
        {
            var regex = new Regex(value.Replace(@"\", @"\\", StringComparison.Ordinal), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return item => regex.IsMatch(item?.ToString() ?? string.Empty);
        }
        catch (ArgumentException)
        {
        }

        return null;
    }

    // Keep for backward compatibility with any external callers
    public static Predicate<object>? BuildFilter(string? value) => BuildTextFilter(value);

    protected override void OnAttached()
    {
        base.OnAttached();

        Filter_Changed();
    }
}
