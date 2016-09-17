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
            Contract.Requires(entries != null);
            Contract.Ensures(Contract.Result<IList<IList<string>>>() != null);

            var languages = entries.SelectMany(e => e.Container.Languages)
                .Select(l => l.CultureKey)
                .Distinct()
                .ToArray();

            var table = languages.GetTableHeaderLines().Concat(entries.GetTableDataLines(languages)).ToArray();

            return table;
        }

        private static IEnumerable<string> GetTableLanguageColumnHeaders(this CultureKey cultureKey)
        {
            Contract.Requires(cultureKey != null);

            var cultureName = cultureKey.ToString();

            yield return CommentHeaderPrefix + cultureName;
            yield return cultureName;
        }

        private static IEnumerable<string> GetTableDataColumns(this ResourceTableEntry entry, CultureKey cultureKey)
        {
            yield return entry.Comments.GetValue(cultureKey);
            yield return entry.Values.GetValue(cultureKey);
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages"></param>
        /// <returns>The header line.</returns>
        private static IEnumerable<IList<string>> GetTableHeaderLines(this IEnumerable<CultureKey> languages)
        {
            Contract.Requires(languages != null);

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
            Contract.Requires(entries != null);
            Contract.Requires(languages != null);

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
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);

            return new[] { entry.Key }.Concat(languages.SelectMany(entry.GetTableDataColumns));
        }

        private static string GetLanguageName(string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            var languageName = dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase)
                ? dataColumnHeader.Substring(CommentHeaderPrefix.Length) : dataColumnHeader;
            return languageName;
        }

        private static CultureInfo ExtractCulture(this string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            return GetLanguageName(dataColumnHeader).ToCulture();
        }

        private static CultureKey ExtractCultureKey(this string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            return GetLanguageName(dataColumnHeader).ToCultureKey();
        }

        private static ColumnKind GetColumnKind(this string dataColumnHeader)
        {
            Contract.Requires(dataColumnHeader != null);

            return dataColumnHeader.StartsWith(CommentHeaderPrefix, StringComparison.OrdinalIgnoreCase) ? ColumnKind.Comment : ColumnKind.Text;
        }

        private static string GetEntryData(this ResourceTableEntry entry, CultureKey culture, ColumnKind columnKind)
        {
            Contract.Requires(entry != null);
            Contract.Requires(culture != null);

            var snapshot = entry.Snapshot;

            if (snapshot != null)
            {
                ResourceData data;
                if (!snapshot.TryGetValue(culture, out data) || (data == null))
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

        private static bool SetEntryData(this ResourceTableEntry entry, CultureInfo culture, ColumnKind columnKind, string text)
        {
            Contract.Requires(entry != null);

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
        public static void ImportTable(this ResourceEntity entity, IList<IList<string>> table)
        {
            Contract.Requires(entity != null);
            Contract.Requires(table != null);

            entity.ImportTable(_fixedColumnHeaders, table).Apply();
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

        public static ICollection<EntryChange> ImportTable(this ResourceEntity entity, ICollection<string> fixedColumnHeaders, IList<IList<string>> table)
        {
            Contract.Requires(entity != null);
            Contract.Requires(fixedColumnHeaders != null);
            Contract.Requires(table != null);
            Contract.Ensures(Contract.Result<ICollection<EntryChange>>() != null);

            if (!table.Any())
                return new EntryChange[0];

            var headerColumns = GetHeaderColumns(table, fixedColumnHeaders);

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;
            var dataColumnCount = headerColumns.Count - fixedColumnHeadersCount;

            var dataColumnHeaders = headerColumns
                .Skip(fixedColumnHeadersCount)
                .Take(dataColumnCount)
                .ToArray();

            dataColumnCount = dataColumnHeaders.Length;

            VerifyCultures(entity, dataColumnHeaders);

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
                .Select(mapping => new EntryChange(mapping.Entry, mapping.Text, mapping.Culture, mapping.ColumnKind, mapping.Entry.GetEntryData(mapping.Culture, mapping.ColumnKind)))
                .ToArray();

            var changes = mappings
                .Where(mapping => (mapping.OriginalText != mapping.Text) && !string.IsNullOrEmpty(mapping.Text))
                .ToArray();

            return changes;
        }

        private static IList<string> GetHeaderColumns(ICollection<IList<string>> table, ICollection<string> fixedColumnHeaders)
        {
            Contract.Requires(table != null);
            Contract.Requires(table.Count > 0);
            Contract.Requires(fixedColumnHeaders != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            Contract.Ensures(table.Count() == Contract.OldValue(table.Count()));

            var headerColumns = table.First();
            Contract.Assume(headerColumns != null);

            var fixedColumnHeadersCount = fixedColumnHeaders.Count;

            if (headerColumns.Count <= fixedColumnHeadersCount)
                throw new ImportException(Resources.ImportColumnMismatchError);

            if (!headerColumns.Take(fixedColumnHeadersCount).SequenceEqual(fixedColumnHeaders))
                throw new ImportException(Resources.ImportHeaderMismatchError);

            return headerColumns;
        }

        [Pure]
        public static bool HasValidTableHeaderRow(this IList<IList<string>> table)
        {
            Contract.Requires(table != null);
            Contract.Requires(table.Count > 0);

            var headerColumns = table.First();
            Contract.Assume(headerColumns != null);

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

        private static void VerifyCultures(ResourceEntity entity, ICollection<string> dataColumns)
        {
            Contract.Requires(entity != null);
            Contract.Requires(dataColumns != null);

            if (dataColumns.Distinct().Count() != dataColumns.Count)
                throw new ImportException(Resources.ImportDuplicateLanguageError);

            var languages = dataColumns.Select(data => data.ExtractCulture()).Distinct().ToArray();

            var undefinedLanguages = languages.Where(outer => entity.Languages.All(inner => !Equals(outer, inner.Culture))).ToArray();

            var lockedLanguage = undefinedLanguages.FirstOrDefault(language => !entity.CanEdit(language));

            if (lockedLanguage == null)
                return;

            throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportLanguageNotEditable, lockedLanguage));
        }
    }
}
