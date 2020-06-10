namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    public static partial class ResourceEntityExtensions
    {
        private const string KeyColumnHeader = @"Key";
        private const string CommentHeaderPrefix = "Comment";

        [NotNull]
        [ItemNotNull]
        private static readonly string[] _fixedColumnHeaders = { KeyColumnHeader };

        /// <summary>
        /// Converts the entries into table with header line.
        /// </summary>
        /// <param name="entries">The entries.</param>
        /// <returns>The table.</returns>
        [NotNull]
        [ItemNotNull]
        public static IList<IList<string>> ToTable([NotNull][ItemNotNull] this ICollection<ResourceTableEntry> entries)
        {
            var languages = entries.SelectMany(e => e.Container.Languages)
                .Select(l => l.CultureKey)
                .Distinct()
                .ToArray();

            var table = languages.GetTableHeaderLines().Concat(entries.GetTableDataLines(languages)).ToArray();

            return table;
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<string> GetTableLanguageColumnHeaders([NotNull] this CultureKey cultureKey)
        {
            var cultureName = cultureKey.ToString();

            yield return CommentHeaderPrefix + cultureName;
            yield return cultureName;
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<string> GetTableDataColumns([NotNull] this ResourceTableEntry entry, CultureKey? cultureKey)
        {
            yield return entry.Comments.GetValue(cultureKey) ?? string.Empty;
            yield return entry.Values.GetValue(cultureKey) ?? string.Empty;
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages"></param>
        /// <returns>The header line.</returns>
        [NotNull, ItemNotNull]
        private static IEnumerable<IList<string>> GetTableHeaderLines([NotNull, ItemNotNull] this IEnumerable<CultureKey> languages)
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
        [NotNull]
        [ItemNotNull]
        private static IEnumerable<IList<string>> GetTableDataLines([NotNull][ItemNotNull] this IEnumerable<ResourceTableEntry> entries, [NotNull][ItemNotNull] IEnumerable<CultureKey> languages)
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
        [NotNull, ItemNotNull]
        private static IEnumerable<string> GetTableLine([NotNull] this ResourceTableEntry entry, [NotNull][ItemNotNull] IEnumerable<CultureKey> languages)
        {
            return new[] { entry.Key }.Concat(languages.SelectMany(entry.GetTableDataColumns));
        }

        [NotNull]
        private static string GetLanguageName([NotNull] string dataColumnHeader)
        {
            var languageName = dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase)
                ? dataColumnHeader.Substring(CommentHeaderPrefix.Length) : dataColumnHeader;
            return languageName;
        }

        private static CultureInfo? ExtractCulture([NotNull] this string dataColumnHeader)
        {
            return GetLanguageName(dataColumnHeader).ToCulture();
        }

        private static CultureKey? ExtractCultureKey([NotNull] this string dataColumnHeader)
        {
            return GetLanguageName(dataColumnHeader).ToCultureKey();
        }

        private static ColumnKind GetColumnKind([NotNull] this string dataColumnHeader)
        {
            return dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase) ? ColumnKind.Comment : ColumnKind.Text;
        }

        private static string? GetEntryData([NotNull] this ResourceTableEntry entry, [NotNull] CultureKey culture, ColumnKind columnKind)
        {
            var snapshot = entry.Snapshot;

            if (snapshot != null)
            {
                if (!snapshot.TryGetValue(culture, out var data) || (data == null))
                    return null;

                switch (columnKind)
                {
                    case ColumnKind.Text:
                        return data.Text;

                    case ColumnKind.Comment:
                        return data.Comment;

                    default:
                        throw new InvalidOperationException("Invalid Column Kind");
                }
            }

            switch (columnKind)
            {
                case ColumnKind.Text:
                    return entry.Values.GetValue(culture);

                case ColumnKind.Comment:
                    return entry.Comments.GetValue(culture);

                default:
                    throw new InvalidOperationException("Invalid Column Kind");
            }
        }

        private static bool SetEntryData([NotNull] this ResourceTableEntry entry, CultureInfo? culture, ColumnKind columnKind, string? text)
        {
            if (!entry.CanEdit(culture))
                return false;

            switch (columnKind)
            {
                case ColumnKind.Text:
                    return entry.Values.SetValue(culture, text);

                case ColumnKind.Comment:
                    return entry.Comments.SetValue(culture, text);

                default:
                    throw new InvalidOperationException("Invalid Column Kind");
            }
        }

        /// <summary>
        /// Imports a table with header line into the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="table">The text.</param>
        public static void ImportTable([NotNull] this ResourceEntity entity, [NotNull][ItemNotNull] IList<IList<string>> table)
        {
            entity.ImportTable(_fixedColumnHeaders, table).Apply();
        }

        public static bool Apply([NotNull] this EntryChange change)
        {
            return change.Entry.SetEntryData(change.Culture, change.ColumnKind, change.Text);
        }

        public static void Apply([NotNull][ItemNotNull] this ICollection<EntryChange> changes)
        {
            var acceptedChanges = changes
                .TakeWhile(change => change.Apply())
                .ToArray();

            if (acceptedChanges.Length == changes.Count)
                return;

            throw new ImportException(acceptedChanges.Length > 0 ? Resources.ImportFailedPartiallyError : Resources.ImportFailedError);
        }

        [NotNull]
        [ItemNotNull]
        public static ICollection<EntryChange> ImportTable([NotNull] this ResourceEntity entity, [NotNull][ItemNotNull] ICollection<string> fixedColumnHeaders, [NotNull][ItemNotNull] IList<IList<string>> table)
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
                .Where(mapping => mapping.Entry != null)
                .Select(mapping => new EntryChange(mapping.Entry!, mapping.Text, mapping.Culture, mapping.ColumnKind, mapping.Entry!.GetEntryData(mapping.Culture, mapping.ColumnKind)))
                .ToArray();

            var changes = mappings
                .Where(mapping => (mapping.OriginalText != mapping.Text) && !string.IsNullOrEmpty(mapping.Text))
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

        [NotNull]
        [ItemNotNull]
        private static IList<string> GetHeaderColumns([NotNull][ItemNotNull] ICollection<IList<string>> table, [NotNull][ItemNotNull] ICollection<string> fixedColumnHeaders)
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
        public static bool HasValidTableHeaderRow([NotNull, ItemNotNull] this IList<IList<string>> table)
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

        private static void VerifyCultures([NotNull] ResourceEntity entity, [NotNull][ItemNotNull] IEnumerable<CultureInfo?> languages)
        {
            var undefinedLanguages = languages.Where(outer => entity.Languages.All(inner => !Equals(outer, inner.Culture))).ToArray();

            var lockedLanguage = undefinedLanguages.FirstOrDefault(language => !entity.CanEdit(language));

            if (lockedLanguage == null)
                return;

            throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportLanguageNotEditable, lockedLanguage));
        }
    }
}
