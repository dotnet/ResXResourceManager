namespace ResXManager.Model;

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using TomsToolbox.Essentials;

public class ExcelRange
{
    private static readonly Regex _rangeRegex = new(@"(((?<sheetName>\w+)|('(?<sheetName>.*?)'))!)?\$?(?<startColumn>[A-Z]+)\$?(?<startRow>[0-9]*)(:\$?(?<endColumn>[A-Z]+)\$?(?<endRow>[0-9]*))?");

    public ExcelRange(string? definition)
    {
        if (definition.IsNullOrEmpty())
            return;

        var match = _rangeRegex.Match(definition);
        if (!match.Success)
            return;

        var startColumn = match.Groups[@"startColumn"]?.Value;
        var endColumn = match.Groups[@"endColumn"]?.Value;
        var startRow = match.Groups[@"startRow"]?.Value;
        var endRow = match.Groups[@"endRow"]?.Value;

        if (endColumn.IsNullOrEmpty())
            endColumn = startColumn;

        if (endRow.IsNullOrEmpty())
            endRow = startRow;

        StartColumnIndex = ColumnToIndex(startColumn);
        EndColumnIndex = ColumnToIndex(endColumn);
        StartRowIndex = startRow.IsNullOrEmpty() ? 0 : int.Parse(startRow, CultureInfo.InvariantCulture) - 1;
        EndRowIndex = endRow.IsNullOrEmpty() ? int.MaxValue : int.Parse(endRow, CultureInfo.InvariantCulture) - 1;
    }

    public int EndColumnIndex
    {
        get;
    }

    public int StartColumnIndex
    {
        get;
    }

    public int EndRowIndex
    {
        get;
    }

    public int StartRowIndex
    {
        get;
    }

    private static int ColumnToIndex(string? column)
    {
        return column.IsNullOrEmpty() ? 0 : column.Aggregate(0, (current, c) => (current * 26) + (c - 'A') + 1) - 1;
    }
}
