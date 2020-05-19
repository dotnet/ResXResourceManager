namespace ResXManager.Model
{
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    public class ExcelRange
    {
        [NotNull]
        private static readonly Regex _rangeRegex = new Regex(@"(((?<sheetName>\w+)|('(?<sheetName>.*?)'))!)?\$?(?<startColumn>[A-Z]+)\$?(?<startRow>[0-9]*)(:\$?(?<endColumn>[A-Z]+)\$?(?<endRow>[0-9]*))?");

        public ExcelRange([CanBeNull] string definition)
        {
            if (string.IsNullOrEmpty(definition))
                return;

            var match = _rangeRegex.Match(definition);
            if (!match.Success)
                return;

            var startColumn = match.Groups[@"startColumn"]?.Value;
            var endColumn = match.Groups[@"endColumn"]?.Value;
            var startRow = match.Groups[@"startRow"]?.Value;
            var endRow = match.Groups[@"endRow"]?.Value;

            if (string.IsNullOrEmpty(endColumn))
                endColumn = startColumn;

            if (string.IsNullOrEmpty(endRow))
                endRow = startRow;

            StartColumnIndex = ColumnToIndex(startColumn);
            EndColumnIndex = ColumnToIndex(endColumn);
            StartRowIndex = string.IsNullOrEmpty(startRow) ? 0 : int.Parse(startRow, CultureInfo.InvariantCulture) - 1;
            EndRowIndex = string.IsNullOrEmpty(endRow) ? int.MaxValue : int.Parse(endRow, CultureInfo.InvariantCulture) - 1;
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

        private static int ColumnToIndex([CanBeNull] string column)
        {
            return string.IsNullOrEmpty(column) ? 0 : column.Aggregate(0, (current, c) => current * 26 + (c - 'A' + 1)) - 1;
        }
    }
}
