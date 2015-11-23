namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    public static partial class ResourceEntityExtensions
    {
        private const string Quote = "\"";
        private const string ColumnSeparator = "\t";
        private const string TerminatingColumnHeaderText = "!";

        // Add one empty terminating column to workaround the ambiguity problem with Excels algorithm to quote multi-line columns.
        private static readonly string[] TerminatorColumnHeaders = { TerminatingColumnHeaderText };
        private static readonly string[] FixedColumnHeaders = { @"Key" };
        private const string CommentHeaderPrefix = "Comment";

        /// <summary>
        /// Converts the entries into a tab-delimited text table.
        /// </summary>
        /// <param name="entries">The entries.</param>
        /// <returns></returns>
        public static string ToTextTable(this ICollection<ResourceTableEntry> entries)
        {
            Contract.Requires(entries != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            var languages = entries.SelectMany(e => e.Owner.Languages).Distinct().ToArray();

            var lines = languages.GetTextTableHeaderLines().Concat(entries.GetTextTableDataLines(languages));
            var result = string.Join(Environment.NewLine, lines) + Environment.NewLine;

            Contract.Assert(!string.IsNullOrEmpty(result));

            return result;
        }

        private static IEnumerable<string> GetTextTableLanguageColumnHeaders(this ResourceLanguage language)
        {
            Contract.Requires(language != null);

            var cultureName = language.CultureKey.ToString();

            yield return CommentHeaderPrefix + cultureName;
            yield return cultureName;
        }

        private static IEnumerable<string> GetTextTableDataColumns(this ResourceTableEntry entry, CultureKey cultureKey)
        {
            yield return Quoted(entry.Comments.GetValue(cultureKey));
            yield return Quoted(entry.Values.GetValue(cultureKey));
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages"></param>
        /// <returns>The header line.</returns>
        private static IEnumerable<string> GetTextTableHeaderLines(this IEnumerable<ResourceLanguage> languages)
        {
            Contract.Requires(languages != null);

            var languageColumns = languages.SelectMany(l => l.GetTextTableLanguageColumnHeaders());

            yield return string.Join(ColumnSeparator, FixedColumnHeaders.Concat(languageColumns).Concat(TerminatorColumnHeaders));
        }

        /// <summary>
        /// Gets the text tables data lines.
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="languages"></param>
        /// <returns>The data lines.</returns>
        private static IEnumerable<string> GetTextTableDataLines(this IEnumerable<ResourceTableEntry> entries, IEnumerable<ResourceLanguage> languages)
        {
            Contract.Requires(entries != null);
            Contract.Requires(languages != null);

            return entries.Select(entry => string.Join(ColumnSeparator, entry.GetTextTableLine(languages)) + ColumnSeparator);
        }

        /// <summary>
        /// Gets one text tables line as an array of columns.
        /// </summary>
        /// <param name="entry">The entry for which to generate the line.</param>
        /// <param name="languages">The languages.</param>
        /// <returns>
        /// The columns of this line.
        /// </returns>
        private static IEnumerable<string> GetTextTableLine(this ResourceTableEntry entry, IEnumerable<ResourceLanguage> languages)
        {
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);

            return (new[] { entry.Key }).Concat(languages.SelectMany(l => entry.GetTextTableDataColumns(l.CultureKey)));
        }

        /// <summary>
        /// Quotes the string if necessary.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The properly quoted string.</returns>
        private static string Quoted(string value)
        {
            if (value == null)
                return string.Empty;

            if (value.Any(IsLineFeed))
            {
                return Quote + value.Replace(Quote, Quote + Quote) + Quote;
            }

            return value;
        }

        private static CultureInfo ExtractCulture(this string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            var languageName = (dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase)
                ? dataColumnHeader.Substring(CommentHeaderPrefix.Length) : dataColumnHeader);

            return languageName.ToCulture();
        }

        private static ColumnKind GetColumnKind(this string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            return dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase) ? ColumnKind.Comment : ColumnKind.Data;
        }

        private static string GetEntryData(this ResourceTableEntry entry, CultureInfo culture, ColumnKind columnKind)
        {
            Contract.Requires(entry != null);

            switch (columnKind)
            {
                case ColumnKind.Data:
                    return entry.Values.GetValue(culture);

                case ColumnKind.Comment:
                    return entry.Comments.GetValue(culture);

                default:
                    throw new InvalidOperationException("Invalid Column Kind");
            }
        }

        private static bool SetEntryData(this ResourceTableEntry entry, CultureInfo culture, ColumnKind columnKind, string text)
        {
            Contract.Requires(entry != null);

            switch (columnKind)
            {
                case ColumnKind.Data:
                    return entry.Values.SetValue(culture, text);

                case ColumnKind.Comment:
                    return entry.Comments.SetValue(culture, text);

                default:
                    throw new InvalidOperationException("Invalid Column Kind");
            }
        }

        /// <summary>
        /// Imports a text table into the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="text">The text.</param>
        public static void ImportTextTable(this ResourceEntity entity, string text)
        {
            Contract.Requires(entity != null);

            if (string.IsNullOrEmpty(text))
                throw new ImportException(Resources.ClipboardIsEmpty);

            var lines = ReadLines(text).ToArray();

            entity.ImportTable(FixedColumnHeaders, lines).Apply();
        }

        public static bool Apply(this EntryChange change)
        {
            Contract.Requires(change != null);

            return change.Entry.SetEntryData(change.Culture, change.ColumnKind, change.Text);
        }

        public static void Apply(this ICollection<EntryChange> changes)
        {
            Contract.Requires(changes != null);

            var acceptedChanges = changes
                .TakeWhile(change => change.Apply())
                .ToArray();

            if (acceptedChanges.Length == changes.Count)
                return;

            throw new ImportException(acceptedChanges.Length > 0 ? Resources.ImportFailedPartiallyError : Resources.ImportFailedError);
        }

        public static ICollection<EntryChange> ImportTable(this ResourceEntity entity, ICollection<string> fixedColumnHeaders, IList<IList<string>> lines)
        {
            Contract.Requires(entity != null);
            Contract.Requires(fixedColumnHeaders != null);
            Contract.Requires(lines != null);
            Contract.Ensures(Contract.Result<ICollection<EntryChange>>() != null);

            if (!lines.Any())
                return new EntryChange[0];

            var headerColumns = GetHeaderColumns(lines, fixedColumnHeaders);

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;
            var emptyColumnCount = GetEmptyColumnCount(lines);
            var dataColumnCount = headerColumns.Count() - fixedColumnHeadersCount - emptyColumnCount;

            var dataColumnHeaders = headerColumns
                .Skip(fixedColumnHeadersCount)
                .Take(dataColumnCount)
                .TakeWhile(header => header != TerminatingColumnHeaderText)
                .ToArray();

            dataColumnCount = dataColumnHeaders.Length;

            VerifyCultures(entity, dataColumnHeaders);

            var mappings = lines.Skip(1)
                .Select(columns => new { Key = columns[0], TextColumns = columns.Skip(fixedColumnHeadersCount).Take(dataColumnCount).ToArray() })
                .Where(mapping => !string.IsNullOrEmpty(mapping.Key))
                .SelectMany(mapping => mapping.TextColumns.Select((column, index) =>
                    new
                    {
                        mapping.Key,
                        Entry = entity.Entries.SingleOrDefault(e => e.Key == mapping.Key) ?? entity.Add(mapping.Key),
                        Text = column,
                        Culture = dataColumnHeaders[index].ExtractCulture(),
                        ColumnKind = dataColumnHeaders[index].GetColumnKind()
                    }))
                .Where(mapping => mapping.Entry != null)
                .Select(mapping => new EntryChange(mapping.Entry, mapping.Text, mapping.Culture, mapping.ColumnKind, mapping.Entry.GetEntryData(mapping.Culture, mapping.ColumnKind)))
                .ToArray();

            var changes = mappings
                .Where(mapping => (mapping.OriginalText != mapping.Text) && !string.IsNullOrEmpty(mapping.Text))
                .ToArray();

            return changes;
        }

        [ContractVerification(false)]
        private static IList<string> GetHeaderColumns(IEnumerable<IList<string>> lines, ICollection<string> fixedColumnHeaders)
        {
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            Contract.Ensures(lines.Count() == Contract.OldValue(lines.Count()));

            var headerColumns = lines.First();

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;

            if (headerColumns.Count() <= fixedColumnHeadersCount)
                throw new ImportException(Resources.ImportColumnMismatchError);

            if (!headerColumns.Take(fixedColumnHeadersCount).SequenceEqual(fixedColumnHeaders))
                throw new ImportException(Resources.ImportHeaderMismatchError);

            return headerColumns;
        }

        private static void VerifyCultures(ResourceEntity entity, IList<string> dataColumns)
        {
            Contract.Requires(entity != null);
            Contract.Requires(dataColumns != null);

            if (dataColumns.Distinct().Count() != dataColumns.Count())
                throw new ImportException(Resources.ImportDuplicateLanguageError);

            var languages = dataColumns.Select(data => data.ExtractCulture()).Distinct().ToArray();

            var undefinedLanguages = languages.Where(outer => entity.Languages.All(inner => !Equals(outer, inner.Culture))).ToArray();

            var lockedLanguage = undefinedLanguages.FirstOrDefault(language => !entity.CanEdit(language));

            if (lockedLanguage == null)
                return;

            throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportLanguageNotEditable, lockedLanguage));
        }

        private static IEnumerable<IList<string>> ReadLines(string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<IEnumerable<IList<string>>>() != null);
            Contract.Ensures(Contract.Result<IEnumerable<IList<string>>>().Any());

            var lines = new List<IList<string>>();

            using (var textEnumerator = new TextEnumerator(text.GetEnumerator()))
            {
                while (textEnumerator.HasData)
                {
                    var columns = ReadLine(textEnumerator);
                    if (!columns.All(string.IsNullOrWhiteSpace))
                    {
                        lines.Add(columns);
                    }
                }
            }

            if (!lines.Any())
                throw new ImportException(Resources.ImportParseEmptyTextError);

            var headerColumns = lines.First();

            if (lines.Any(columns => columns.Count() != headerColumns.Count()))
                throw new ImportException(Resources.ImportNormalizedTableExpected);

            return lines;
        }

        private static IList<string> ReadLine(TextEnumerator textEnumerator)
        {
            Contract.Requires(textEnumerator != null);

            var columns = new List<string>();

            while (textEnumerator.HasData)
            {
                columns.Add(ReadColumn(textEnumerator));

                if (IsTab(textEnumerator.Current))
                {
                    textEnumerator.Skip(1);
                    continue;
                }

                if (!IsLineFeed(textEnumerator.Current))
                {
                    throw new ImportException(Resources.ImportInconsistentDoubleQuoteError);
                }

                textEnumerator.SkipWhile(IsLineFeed);

                break;
            }

            return columns.ToArray();
        }

        private static string ReadColumn(TextEnumerator textEnumerator)
        {
            Contract.Requires(textEnumerator != null);
            // Excels quoting is sometimes ambigous - try to do our best.
            // Should be OK if we have an empty column at the end.

            if (IsDoubleQuote(textEnumerator.Current))
            {
                textEnumerator.Skip(1);

                var text = textEnumerator.Take((current, next) =>
                    IsTab(current) ? 0 : // Whenever we reach a tab the column is finshed.
                    ((IsDoubleQuote(current) && IsDoubleQuote(next)) ? 2 : // Read both of doubled double quotes.
                    ((IsDoubleQuote(current) && IsTabOrLineFeed(next)) ? 0 : // Stop at a single double quote followed by new line or tab.
                    1))); // Else read the char.

                if (IsDoubleQuote(textEnumerator.Current))
                {
                    textEnumerator.Skip(1);
                    if (text.Any(IsLineFeed))
                    {
                        // Only if text contains a line break the surrounding quotes are the result of automatic quoting and containing quotes are doubled...
                        text = text.Replace(Quote + Quote, Quote);
                    }
                    else
                    {
                        // ... else restore the surrounding quotes, they have been part of the original text.
                        text = Quote + text + Quote;
                    }
                }
                else
                {
                    text = text.Insert(0, Quote);
                }

                return text;
            }

            return textEnumerator.TakeUntil(IsTabOrLineFeed);
        }

        private static bool IsDoubleQuote(char c)
        {
            return (c == '"');
        }

        private static bool IsTabOrLineFeed(char c)
        {
            return IsTab(c) || IsLineFeed(c);
        }

        private static bool IsLineFeed(char c)
        {
            return (c == '\r') || (c == '\n');
        }

        private static bool IsTab(char c)
        {
            return (c == '\t');
        }

        private static int GetEmptyColumnCount(IList<IList<string>> lines)
        {
            Contract.Requires(lines != null);
            Contract.Requires(lines.Any());

            var emptyColumnCount = Enumerable.Range(0, lines.First().Count())
                .Skip(2)
                .Reverse()
                .TakeWhile(columnIndex => lines.All(columns => string.IsNullOrWhiteSpace(columns[columnIndex])))
                .Count();

            var terminatorColumnCount = lines.First().Skip(2).Reverse().SkipWhile(string.IsNullOrWhiteSpace).TakeWhile(c => TerminatorColumnHeaders.Any(h => (c == h))).Count();

            return emptyColumnCount + terminatorColumnCount;
        }
    }
}
