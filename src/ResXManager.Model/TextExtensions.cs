namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    public static class TextExtensions
    {
        private const string Quote = "\"";

        /// <summary>
        /// The text column separator
        /// </summary>
        private const char TextColumnSeparator = '\t';

        [NotNull]
        public static string ToTextString([NotNull, ItemCanBeNull] this IList<IList<string?>?> table, char separator = TextColumnSeparator)
        {
            IList<string?>? row;

            if ((table.Count == 1) && ((row = table[0]) != null) && (row.Count == 1) && string.IsNullOrWhiteSpace(row[0]))
                return Quote + (row[0] ?? string.Empty) + Quote;

            return string.Join(Environment.NewLine, table.Select(line => string.Join(separator.ToString(CultureInfo.InvariantCulture), line?.Select(cell => Quoted(cell, separator)) ?? Enumerable.Empty<string>())));
        }

        [NotNull]
        private static string Quoted(string? value, char separator)
        {
            if (value == null || string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Any(IsLineFeed) || value.Contains(separator) || value.StartsWith(Quote, StringComparison.Ordinal))
            {
                return Quote + value.Replace(Quote, Quote + Quote) + Quote;
            }

            return value;
        }

        [ItemNotNull]
        internal static IList<IList<string>>? ParseTable([NotNull] this string text, char separator = TextColumnSeparator)
        {
            var table = new List<IList<string>>();

            using (var reader = new StringReader(text))
            {
                while (reader.Peek() != -1)
                {
                    table.Add(ReadTableLine(reader, separator));
                }
            }

            if (!table.Any())
                return null;

            var headerColumns = table.First();

            return table.Any(columns => columns?.Count != headerColumns?.Count) ? null : table;
        }

        [NotNull, ItemNotNull]
        private static IList<string> ReadTableLine([NotNull] TextReader reader, char separator)
        {

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
            return columns;
        }

        [NotNull]
        internal static string ReadTableColumn([NotNull] TextReader reader, char separator)
        {

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
                    else
                    {
                        stringBuilder.Append((char)nextChar);
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
