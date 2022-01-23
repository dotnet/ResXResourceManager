﻿namespace ResXManager.View.Converters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

using ResXManager.View.Tools;

public sealed class IsCellSelectionInvariantConverter : IValueConverter
{
    public static readonly IsCellSelectionInvariantConverter Default = new();

    object? IValueConverter.Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        return (value as IEnumerable<DataGridCellInfo>)?.Any(item => item.IsItemInvariant());
    }

    object? IValueConverter.ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
