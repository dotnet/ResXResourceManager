namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    public static partial class ResourceEntityExtensions
    {
        private const string KeyColumnHeader = @"Key";
        private const string CommentHeaderPrefix = "Comment";

        private static readonly string[] _fixedColumnHeaders = { KeyColumnHeader };

        /// <summary>
        /// Converts the entries into table with header line.
        /// </summary>
        /// <param name="entries">The entries.</param>
        /// <returns>The table.</returns>
        public static IList<IList<string>> ToTable(this ICollection<ResourceTableEntry> entries)
        {
            var languages = entries.SelectMany(e => e.Container.Languages)
                .Select(l => l.CultureKey)
                .Distinct()
                .ToArray();

            var table = languages.GetTableHeaderLines().Concat(entries.GetTableDataLines(languages)).ToArray();

            return table;
        }

        private static IEnumerable<string> GetTableLanguageColumnHeaders(this CultureKey cultureKey)
        {
            var cultureName = cultureKey.ToString();

            yield return CommentHeaderPrefix + cultureName;
            yield return cultureName;
        }

        private static IEnumerable<string> GetTableDataColumns(this ResourceTableEntry entry, CultureKey? cultureKey)
        {
            yield return entry.Comments.GetValue(cultureKey) ?? string.Empty;
            yield return entry.Values.GetValue(cultureKey) ?? string.Empty;
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages"></param>
        /// <returns>The header line.</returns>
        private static IEnumerable<IList<string>> GetTableHeaderLines(this IEnumerable<CultureKey> languages)
        {
            var languageColumns = languages.SelectMany(l => l.GetTableLanguageColumnHeaders());

            yield return _fixedColumnHeaders.Concat(languageColumns).ToArray();
        }

        /// <summary>
        /// Gets the text tables data table.
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="languages"></param>
        /// <returns>The data table.</returns>
        private static IEnumerable<IList<string>> GetTableDataLines(this IEnumerable<ResourceTableEntry> entries, IEnumerable<CultureKey> languages)
        {
            return entries.Select(entry => entry.GetTableLine(languages).ToArray());
        }

        /// <summary>
        /// Gets one text tables line as an array of columns.
        /// </summary>
        /// <param name="entry">The entry for which to generate the line.</param>
        /// <param name="languages">The languages.</param>
        /// <returns>
        /// The columns of this line.
        /// </returns>
        private static IEnumerable<string> GetTableLine(this ResourceTableEntry entry, IEnumerable<CultureKey> languages)
        {
            return new[] { entry.Key }.Concat(languages.SelectMany(entry.GetTableDataColumns));
        }

        private static string GetLanguageName(string dataColumnHeader)
        {
            var languageName = dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase)
                ? dataColumnHeader.Substring(CommentHeaderPrefix.Length) : dataColumnHeader;
            return languageName;
        }

        private static CultureInfo? ExtractCulture(this string dataColumnHeader)
        {
            return GetLanguageName(dataColumnHeader).ToCulture();
        }

        private static CultureKey? ExtractCultureKey(this string dataColumnHeader)
        {
            return GetLanguageName(dataColumnHeader).ToCultureKey();
        }

        private static ColumnKind GetColumnKind(this string dataColumnHeader)
        {
            return dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase) ? ColumnKind.Comment : ColumnKind.Text;
        }

        private static string? GetEntryData(this ResourceTableEntry entry, CultureKey culture, ColumnKind columnKind)
        {
            var snapshot = entry.Snapshot;

            if (snapshot != null)
            {
                if (!snapshot.TryGetValue(culture, out var data) || (data == null))
                    return null;

                return columnKind switch
                {
                    ColumnKind.Text => data.Text,
                    ColumnKind.Comment => data.Comment,
                    _ => throw new InvalidOperationException("Invalid Column Kind")
                };
            }

            return columnKind switch
            {
                ColumnKind.Text => entry.Values.GetValue(culture),
                ColumnKind.Comment => entry.Comments.GetValue(culture),
                _ => throw new InvalidOperationException("Invalid Column Kind")
            };
        }

        private static bool SetEntryData(this ResourceTableEntry entry, CultureInfo? culture, ColumnKind columnKind, string? text)
        {
            if (!entry.CanEdit(culture))
                return false;

            switch (columnKind)
            {
                case ColumnKind.Text:
                    entry.Values.SetValue(culture, text);
                    break;

                case ColumnKind.Comment:
                    entry.Comments.SetValue(culture, text);
                    break;

                default:
                    throw new InvalidOperationException("Invalid Column Kind");
            }

            return true;
        }

        /// <summary>
        /// Imports a table with header line into the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="table">The text.</param>
        public static void ImportTable(this ResourceEntity entity, IList<IList<string>> table)
        {
            entity.ImportTable(_fixedColumnHeaders, table).Apply();
        }

        public static bool Apply(this EntryChange change)
        {
            return change.Entry.SetEntryData(change.Culture, change.ColumnKind, change.Text);
        }

        public static void Apply(this ICollection<EntryChange> changes)
        {
            var acceptedChanges = changes
                .TakeWhile(change => change.Apply())
                .ToArray();

            if (acceptedChanges.Length == changes.Count)
                return;

            throw new ImportException(acceptedChanges.Length > 0 ? Resources.ImportFailedPartiallyError : Resources.ImportFailedError);
        }

        public static ICollection<EntryChange> ImportTable(this ResourceEntity entity, ICollection<string> fixedColumnHeaders, IList<IList<string>> table)
        {
            if (!table.Any())
                return Array.Empty<EntryChange>();

            table = table.NormalizeTable();

            var headerColumns = GetHeaderColumns(table, fixedColumnHeaders);

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;
            var dataColumnCount = headerColumns.Count - fixedColumnHeadersCount;

            var dataColumnHeaders = headerColumns
                .Skip(fixedColumnHeadersCount)
                .Take(dataColumnCount)
                .ToArray();

            dataColumnCount = dataColumnHeaders.Length;

            if (dataColumnHeaders.Distinct().Count() != dataColumnHeaders.Length)
                throw new ImportException(Resources.ImportDuplicateLanguageError);

            var mappings = table.Skip(1)
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
                .Where(mapping => mapping.Entry != null);

            // ! mapping.Entry is checked in Where(...)
            var entries = mappings
                .Select(mapping => new EntryChange(mapping.Entry!, mapping.Text, mapping.Culture, mapping.ColumnKind, mapping.Entry!.GetEntryData(mapping.Culture, mapping.ColumnKind)))
                .ToArray();

            var changes = entries
                .Where(entryChange => entryChange.IsModified())
                .ToArray();

            VerifyCultures(entity, changes.Select(c => c.Culture).Distinct());

            return changes;
        }

        private static IList<IList<string>> NormalizeTable(this IList<IList<string>> table)
        {
            if (!table.Any())
                return table;

            var columnSizes = table
                .Select(row => row.Count - row.Reverse().TakeWhile(string.IsNullOrWhiteSpace).Count())
                .ToArray();

            var columns = columnSizes.Max();

            var normalized = table
                .Select(row => NormalizeRow(row, columns))
                .ToArray();

            return normalized;
        }

        private static IList<string> NormalizeRow(IList<string> row, int expected)
        {
            var current = row.Count;

            if (current > expected)
                return row
                    .Take(expected)
                    .ToArray();

            if (current < expected)
                return row
                    .Concat(Enumerable.Repeat(string.Empty, expected - current))
                    .ToArray();

            return row;
        }

        private static IList<string> GetHeaderColumns(ICollection<IList<string>> table, ICollection<string> fixedColumnHeaders)
        {
            var headerColumns = table.First();

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;

            if (headerColumns.Count <= fixedColumnHeadersCount)
                throw new ImportException(Resources.ImportColumnMismatchError);

            if (!headerColumns.Take(fixedColumnHeadersCount).SequenceEqual(fixedColumnHeaders))
                throw new ImportException(Resources.ImportHeaderMismatchError);

            return headerColumns;
        }

        [System.Diagnostics.Contracts.Pure]
        public static bool HasValidTableHeaderRow(this IList<IList<string>> table)
        {
            if (table.Count == 0)
                return false;

            var headerColumns = table.First();

            if (headerColumns.Count < 2)
                return false;

            if (headerColumns[0] != KeyColumnHeader)
                return false;

            var headerCultures = headerColumns
                .Skip(1)
                .Select(ExtractCultureKey)
                .ToArray();

            return headerCultures.All(c => c != null);
        }

        private static void VerifyCultures(ResourceEntity entity, IEnumerable<CultureInfo?> languages)
        {
            var undefinedLanguages = languages.Where(outer => entity.Languages.All(inner => !Equals(outer, inner.Culture))).ToArray();

            var lockedLanguage = undefinedLanguages.FirstOrDefault(language => !entity.CanEdit(language));

            if (lockedLanguage == null)
                return;

            throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportLanguageNotEditable, lockedLanguage));
        }
    }
}
