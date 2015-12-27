namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Core;

    public static class ClipboardHelper
    {
        private const string Quote = "\"";

        private const char TextColumnSeparator = '\t';

        private static char CsvColumnSeparator
        {
            get
            {
                return CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "," ? ';' : ',';
            }
        }

        public static string ToTextString(this IList<IList<string>> table)
        {
            Contract.Requires(table != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return ToString(table, TextColumnSeparator);
        }

        public static string ToCsvString(this IList<IList<string>> table)
        {
            Contract.Requires(table != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return ToString(table, CsvColumnSeparator);
        }

        public static IList<IList<string>> GetClipboardData()
        {
            Contract.Ensures(Contract.Result<IList<IList<string>>>() != null);
            Contract.Ensures(Contract.Result<IList<IList<string>>>().Count > 0);
            Contract.Ensures(Contract.ForAll(Contract.Result<IList<IList<string>>>(), item => item != null));

            var csv = Clipboard.GetData(DataFormats.CommaSeparatedValue) as string;
            if (!string.IsNullOrEmpty(csv))
                return ReadTableLines(csv, CsvColumnSeparator);

            var text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                throw new ImportException(Resources.ClipboardIsEmpty);

            return ReadTableLines(text, TextColumnSeparator);
        }

        public static void SetClipboardData(this IList<IList<string>> table)
        {
            if (table == null)
            {
                Clipboard.Clear();
                return;
            }

            var textString = table.ToTextString();
            var csvString = table.ToCsvString();

            var dataObject = new DataObject();

            dataObject.SetText(textString);
            dataObject.SetText(csvString, TextDataFormat.CommaSeparatedValue);

            Clipboard.SetDataObject(dataObject);
        }

        private static string ToString(this IList<IList<string>> table, char separator)
        {
            Contract.Requires(table != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return string.Join(Environment.NewLine, table.Select(line => string.Join(separator.ToString(), line.Select(cell => Quoted(cell, separator)))));
        }

        private static string Quoted(string value, char separator)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Any(IsLineFeed) || value.Contains(separator) || value.StartsWith(Quote))
            {
                return Quote + value.Replace(Quote, Quote + Quote) + Quote;
            }

            return value;
        }

        public static IList<IList<string>> ReadTableLines(string text, char separator)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<IList<IList<string>>>() != null);
            Contract.Ensures(Contract.Result<IList<IList<string>>>().Count > 0);
            Contract.Ensures(Contract.ForAll(Contract.Result<IList<IList<string>>>(), item => item != null));

            var table = new List<IList<string>>();

            using (var reader = new StringReader(text))
            {
                while (reader.Peek() != -1)
                {
                    table.Add(ReadTableLine(reader, separator));
                }
            }

            if (!table.Any())
                throw new ImportException(Resources.ImportParseEmptyTextError);

            var headerColumns = table.First();

            if (table.Any(columns => columns.Count != headerColumns.Count))
                throw new ImportException(Resources.ImportNormalizedTableExpected);

            Contract.Assume(Contract.ForAll(table, item => item != null));

            return table;
        }

        private static IList<string> ReadTableLine(TextReader reader, char separator)
        {
            Contract.Requires(reader != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            Contract.Ensures(Contract.ForAll(Contract.Result<IList<string>>(), item => item != null));

            var columns = new List<string>();

            while (true)
            {
                columns.Add(ReadTableColumn(reader, separator));

                if ((char)reader.Peek() == separator)
                {
                    reader.Read();
                    continue;
                }

                while (IsLineFeed(reader.Peek()))
                {
                    reader.Read();
                }

                break;
            }

            Contract.Assume(Contract.ForAll(columns, item => item != null));
            return columns;
        }

        private static string ReadTableColumn(TextReader reader, char separator)
        {
            Contract.Requires(reader != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var stringBuilder = new StringBuilder();
            int nextChar;

            if (IsDoubleQuote(reader.Peek()))
            {
                reader.Read();

                while ((nextChar = reader.Read()) != -1)
                {
                    if (IsDoubleQuote(nextChar))
                    {
                        if (IsDoubleQuote(reader.Peek()))
                        {
                            reader.Read();
                            stringBuilder.Append((char)nextChar);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                while ((nextChar = reader.Peek()) != -1)
                {
                    if (IsLineFeed(nextChar) || (nextChar == separator))
                        break;

                    reader.Read();
                    stringBuilder.Append((char)nextChar);
                }
            }

            return stringBuilder.ToString();
        }

        private static bool IsDoubleQuote(int c)
        {
            return (c == '"');
        }

        private static bool IsLineFeed(int c)
        {
            return (c == '\r') || (c == '\n');
        }

        private static bool IsLineFeed(char c)
        {
            return IsLineFeed((int)c);
        }
    }
}
