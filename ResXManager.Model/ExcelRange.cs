namespace tomenglertde.ResXManager.Model
{
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ExcelRange
    {
        private static readonly Regex RangeRegex = new Regex(@"((?<sheetName>\w+)|('(?<sheetName>.*?)')!)?\$?(?<startColumn>[A-Z]+)\$?(?<startRow>[0-9]*)(:\$?(?<endColumn>[A-Z]+)\$?(?<endRow>[0-9]*))?");

        public ExcelRange(string definition)
        {
            if (string.IsNullOrEmpty(definition))
                return;

            var match = RangeRegex.Match(definition);
            if (!match.Success)
                return;

            SheetName = match.GetGroupValue(@"sheetName");
            StartColumn = match.GetGroupValue(@"startColumn");
            EndColumn = match.GetGroupValue(@"endColumn");
            StartRow = match.GetGroupValue(@"startRow");
            EndRow = match.GetGroupValue(@"endRow");

            if (string.IsNullOrEmpty(EndColumn))
                EndColumn = StartColumn;

            if (string.IsNullOrEmpty(EndRow))
                EndRow = StartRow;

            StartColumnIndex = ColumnToIndex(StartColumn);
            EndColumnIndex = ColumnToIndex(EndColumn);
            StartRowIndex = string.IsNullOrEmpty(StartRow) ? 0 : int.Parse(StartRow, CultureInfo.InvariantCulture) - 1;
            EndRowIndex = string.IsNullOrEmpty(EndRow) ? int.MaxValue : int.Parse(EndRow, CultureInfo.InvariantCulture) - 1;
        }

        public int EndColumnIndex
        {
            get;
            private set;
        }

        public int StartColumnIndex
        {
            get;
            private set;
        }

        public int EndRowIndex
        {
            get;
            private set;
        }

        public int StartRowIndex
        {
            get;
            private set;
        }

        public string EndRow
        {
            get;
            private set;
        }

        public string StartRow
        {
            get;
            private set;
        }

        public string EndColumn
        {
            get;
            private set;
        }

        public string StartColumn
        {
            get;
            private set;
        }

        public string SheetName
        {
            get;
            private set;
        }

        private static int ColumnToIndex(string column)
        {
            return column.Aggregate(0, (current, c) => current * 26 + (c - 'A'));
        }
    }
}
