namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using ResXManager.Infrastructure;
using TomsToolbox.Essentials;

public static class TextExtensions
{
    private const string Quote = "\"";

    /// <summary>
    /// The text column separator
    /// </summary>
    private const char TextColumnSeparator = '\t';

    public static string ToTextString(this IList<IList<string?>?> table, char separator = TextColumnSeparator)
    {
        IList<string?>? row;

        if ((table.Count == 1) && ((row = table[0]) != null) && (row.Count == 1) && string.IsNullOrWhiteSpace(row[0]))
            return Quote + (row[0] ?? string.Empty) + Quote;

        return string.Join(Environment.NewLine, table.Select(line => string.Join(separator.ToString(CultureInfo.InvariantCulture), line?.Select(cell => Quoted(cell, separator)) ?? Enumerable.Empty<string>())));
    }

    private static string Quoted(string? value, char separator)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        if (value.Any(IsLineFeed) || value.Contains(separator, StringComparison.Ordinal) || value.StartsWith(Quote, StringComparison.Ordinal))
        {
            return Quote + value.Replace(Quote, Quote + Quote, StringComparison.Ordinal) + Quote;
        }

        return value;
    }

    internal static IList<IList<string>>? ParseTable(this string text, char separator = TextColumnSeparator)
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

    private static IList<string> ReadTableLine(TextReader reader, char separator)
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

    internal static string ReadTableColumn(TextReader reader, char separator)
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
