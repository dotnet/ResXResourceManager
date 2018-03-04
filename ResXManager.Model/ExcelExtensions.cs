namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    public enum ExcelExportMode
    {
        [LocalizedDisplayName(StringResourceKey.ExcelExport_SingleSheet)]
        SingleSheet,
        [LocalizedDisplayName(StringResourceKey.ExcelExport_MultipleSheets)]
        MultipleSheets
    }

    public static partial class ResourceEntityExtensions
    {
        [NotNull, ItemNotNull]
        private static readonly string[] _singleSheetFixedColumnHeaders = { "Project", "File", "Key" };

        public static void ExportExcelFile([NotNull] this ResourceManager resourceManager, [NotNull] string filePath, [CanBeNull] IResourceScope scope, ExcelExportMode exportMode)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

            using (var package = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                Contract.Assume(package != null);

                var workbookPart = package.AddWorkbookPart();
                Contract.Assume(workbookPart != null);

                if (exportMode == ExcelExportMode.MultipleSheets)
                {
                    ExportToMultipleSheets(resourceManager, workbookPart, scope);
                }
                else
                {
                    ExportToSingleSheet(workbookPart, scope ?? new FullScope(resourceManager.ResourceEntities));
                }
            }
        }

        private static void ExportToMultipleSheets([NotNull] ResourceManager resourceManager, [NotNull] WorkbookPart workbookPart, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(workbookPart != null);

            var entitiesQuery = GetMultipleSheetEntities(resourceManager);

            if (scope != null)
            {
                var entitiesInScope = scope.Entries.Select(entry => entry.Container).Distinct().ToArray();
                entitiesQuery = entitiesQuery.Where(entity => entitiesInScope.Contains(entity.ResourceEntity));
            }

            var entities = entitiesQuery.ToArray();

            workbookPart.Workbook = new Workbook().AppendItem(entities.Aggregate(new Sheets(), (seed, item) => seed.AppendItem(item.CreateSheet())));

            foreach (var item in entities)
            {
                Contract.Assume(item != null);

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>(item.Id);
                Contract.Assume(worksheetPart != null);
                worksheetPart.Worksheet = new Worksheet().AppendItem(item.GetDataRows(scope).Aggregate(new SheetData(), AppendRow));
            }
        }

        private static void ExportToSingleSheet([NotNull] WorkbookPart workbookPart, [NotNull] IResourceScope scope)
        {
            Contract.Requires(workbookPart != null);
            Contract.Requires(scope != null);

            var languages = scope.Languages.Concat(scope.Comments).Distinct().ToArray();
            var entries = scope.Entries.ToArray();

            var sheet = new Sheet { Name = "ResXResourceManager", Id = "Id1", SheetId = 1 };
            workbookPart.Workbook = new Workbook().AppendItem(new Sheets(sheet));
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>(sheet.Id);
            Contract.Assume(worksheetPart != null);
            var worksheet = new Worksheet();

            worksheetPart.Worksheet = worksheet;

            var headerRow = _singleSheetFixedColumnHeaders.Concat(languages.GetLanguageColumnHeaders(scope));
            var dataRows = entries.Select(e => new[] { e.Container.ProjectName, e.Container.UniqueName }.Concat(e.GetDataRow(languages, scope)));

            var rows = new[] { headerRow }.Concat(dataRows);

            worksheet.AppendItem(rows.Aggregate(new SheetData(), AppendRow));
        }

        [NotNull, ItemNotNull]
        public static IList<EntryChange> ImportExcelFile([NotNull] this ResourceManager resourceManager, [NotNull] string filePath)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);
            Contract.Ensures(Contract.Result<IEnumerable<EntryChange>>() != null);

            using (var package = SpreadsheetDocument.Open(filePath, false))
            {
                Contract.Assume(package != null);
                var workbookPart = package.WorkbookPart;
                Contract.Assume(workbookPart != null);

                var workbook = workbookPart.Workbook;
                Contract.Assume(workbook != null);

                var sharedStrings = workbookPart.GetSharedStrings();

                var sheets = workbook.Sheets;
                Contract.Assume(sheets != null);

                var firstSheet = sheets.OfType<Sheet>().FirstOrDefault();
                if (firstSheet == null)
                    return new EntryChange[0];

                var firstRow = firstSheet.GetRows(workbookPart).FirstOrDefault();

                var changes = IsSingleSheetHeader(firstRow, sharedStrings)
                    ? ImportSingleSheet(resourceManager, firstSheet, workbookPart, sharedStrings)
                    : ImportMultipleSheets(resourceManager, sheets, workbookPart, sharedStrings);

                return changes.ToArray();
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<EntryChange> ImportSingleSheet([NotNull] ResourceManager resourceManager, [NotNull, ItemNotNull] Sheet firstSheet, [NotNull] WorkbookPart workbookPart, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(firstSheet != null);
            Contract.Requires(workbookPart != null);
            Contract.Ensures(Contract.Result<IEnumerable<EntryChange>>() != null);

            var data = firstSheet.GetRows(workbookPart).Select(row => row.GetCellValues(sharedStrings)).ToArray();

            var firstRow = data.FirstOrDefault();

            Contract.Assume(firstRow != null);

            var headerRow = (IList<string>)firstRow.Skip(2).ToArray();

            var rowsByProject = data.Skip(1).GroupBy(row => row.FirstOrDefault() ?? string.Empty, row => (IList<string>)row.Skip(1).ToArray());

            foreach (var projectRows in rowsByProject)
            {
                Contract.Assume(projectRows != null);

                var projectName = projectRows.Key;

                if (string.IsNullOrEmpty(projectName))
                    continue;

                var rowsByFile = projectRows.GroupBy(row => row.FirstOrDefault() ?? string.Empty, row => (IList<string>)row.Skip(1).ToArray());

                foreach (var fileRows in rowsByFile)
                {
                    Contract.Assume(fileRows != null);

                    var uniqueName = fileRows.Key;
                    if (string.IsNullOrEmpty(uniqueName))
                        continue;

                    var projectEntities = resourceManager.ResourceEntities
                        .Where(item => projectName.Equals(item.ProjectName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    var entity = projectEntities.FirstOrDefault(item => uniqueName.Equals(item.UniqueName, StringComparison.OrdinalIgnoreCase))
                        // fallback for backward compatibility:
                        ?? projectEntities.FirstOrDefault(item => uniqueName.Equals(item.BaseName, StringComparison.OrdinalIgnoreCase));

                    if (entity == null)
                        continue;

                    var tableData = new[] { headerRow }.Concat(fileRows).ToArray();

                    foreach (var change in entity.ImportTable(_fixedColumnHeaders, tableData))
                    {
                        yield return change;
                    }
                }
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<EntryChange> ImportMultipleSheets([NotNull] ResourceManager resourceManager, [NotNull][ItemNotNull] Sheets sheets, [NotNull] WorkbookPart workbookPart, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(sheets != null);
            Contract.Requires(workbookPart != null);
            Contract.Ensures(Contract.Result<IEnumerable<EntryChange>>() != null);

            var entities = GetMultipleSheetEntities(resourceManager).ToArray();

            var changes = sheets.OfType<Sheet>()
                .SelectMany(sheet => FindResourceEntity(entities, sheet).ImportTable(_fixedColumnHeaders, sheet.GetTable(workbookPart, sharedStrings)));

            return changes;
        }

        [NotNull]
        [ItemNotNull]
        private static IList<string>[] GetTable([NotNull][ItemNotNull] this Sheet sheet, [NotNull] WorkbookPart workbookPart, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(sheet != null);
            Contract.Requires(workbookPart != null);
            Contract.Ensures(Contract.Result<IList<string>[]>() != null);

            return sheet.GetRows(workbookPart)
                .Select(row => row.GetCellValues(sharedStrings))
                .ToArray();
        }

        private static bool IsSingleSheetHeader([ItemNotNull][CanBeNull] Row firstRow, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            return (firstRow != null) && firstRow.GetCellValues(sharedStrings)
                .Take(_singleSheetFixedColumnHeaders.Length)
                .SequenceEqual(_singleSheetFixedColumnHeaders);
        }

        [NotNull]
        [ItemNotNull]
        private static IList<string> GetCellValues([NotNull][ItemNotNull] this Row row, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(row != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);

            return row.OfType<Cell>().GetCellValues(sharedStrings).ToArray();
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<Row> GetRows([NotNull][ItemNotNull] this Sheet sheet, [NotNull] WorkbookPart workbookPart)
        {
            Contract.Requires(sheet != null);
            Contract.Requires(workbookPart != null);
            Contract.Ensures(Contract.Result<IEnumerable<Row>>() != null);

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);

            return worksheetPart?.Worksheet?.ChildElements?.OfType<SheetData>()?.FirstOrDefault()?.OfType<Row>() ?? Enumerable.Empty<Row>();
        }

        [NotNull]
        private static ResourceEntity FindResourceEntity([NotNull][ItemNotNull] this IEnumerable<MultipleSheetEntity> entities, [NotNull][ItemNotNull] Sheet sheet)
        {
            Contract.Requires(entities != null);
            Contract.Requires(sheet != null);
            Contract.Ensures(Contract.Result<ResourceEntity>() != null);

            var name = GetName(sheet);

            var entity = entities.Where(item => item.SheetName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.ResourceEntity)
                .FirstOrDefault();

            if (entity == null)
                throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportMapSheetError, name));

            return entity;
        }

        [ContractVerification(false)]
        [NotNull]
        private static string GetName([NotNull][ItemNotNull] this Sheet sheet)
        {
            Contract.Requires(sheet != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return sheet.Name.Value;
        }

        [ItemNotNull]
        [CanBeNull]
        private static SheetData AppendRow([NotNull][ItemNotNull] SheetData sheetData, [NotNull][ItemNotNull] IEnumerable<string> rowData)
        {
            Contract.Requires(sheetData != null);
            Contract.Requires(rowData != null);

            var row = (sheetData.ChildElements?.OfType<Row>()?.Count() ?? 0) + 1;
            var column = 1;
            return sheetData.AppendItem(rowData.Aggregate(new Row(), (seed, item) => seed.AppendItem(CreateCell(item, row, column++))));
        }

        [NotNull]
        [ItemNotNull]
        private static Cell CreateCell([CanBeNull] string text, int row, int column)
        {
            Contract.Ensures(Contract.Result<Cell>() != null);

            return new Cell
            {
                DataType = CellValues.InlineString,
                CellReference = CreateCellReference(row, column),
                InlineString = new InlineString
                {
                    Text = new Text(text ?? string.Empty)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    }
                }
            };
        }

        [NotNull]
        private static string CreateCellReference(int row, int column)
        {
            return GetExcelColumnName(column) + row.ToString(CultureInfo.InvariantCulture);
        }

        // From: https://stackoverflow.com/questions/181596/how-to-convert-a-column-number-eg-127-into-an-excel-column-eg-aa
        [NotNull]
        private static string GetExcelColumnName(int columnNumber)
        {
            var dividend = columnNumber;
            var columnName = string.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        [ItemNotNull]
        [CanBeNull]
        private static IList<SharedStringItem> GetSharedStrings([NotNull] this WorkbookPart workbookPart)
        {
            Contract.Requires(workbookPart != null);

            var sharedStringsPart = workbookPart.SharedStringTablePart;

            var stringTable = sharedStringsPart?.SharedStringTable;

            return stringTable?.OfType<SharedStringItem>().ToArray();
        }

        [CanBeNull]
        private static string GetText([NotNull][ItemNotNull] this CellType cell, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(cell != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var cellValue = cell.CellValue;
            var dataType = cell.DataType;

            if (cellValue == null)
                return GetTextFromTextElement(cell);

            var text = cellValue.Text;

            if ((dataType != null) && (dataType == CellValues.SharedString) && (sharedStrings != null) && (int.TryParse(text, out var index) && (index >= 0) && (index < sharedStrings.Count)))
            {
                var stringItem = sharedStrings[index];

                return GetTextFromTextElement(stringItem) ?? text;
            }

            return text;

        }

        private static string GetTextFromTextElement([NotNull] OpenXmlElement cell)
        {
            // ReSharper disable AssignNullToNotNullAttribute
            // ReSharper disable PossibleNullReferenceException
            return cell.ChildElements
                .OfType<OpenXmlLeafTextElement>()
                .Select(item => item.Text)
                .FirstOrDefault();
            // ReSharper restore AssignNullToNotNullAttribute
            // ReSharper restore PossibleNullReferenceException
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> GetCellValues([NotNull][ItemNotNull] this IEnumerable<Cell> cells, [ItemNotNull][CanBeNull] IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(cells != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            var columnIndex = 0;

            foreach (var cell in cells)
            {
                Contract.Assume(cell != null);
                var cellColumnIndex = new ExcelRange(cell.CellReference).StartColumnIndex;

                while (columnIndex < cellColumnIndex)
                {
                    yield return string.Empty;
                    columnIndex += 1;
                }

                var text = cell.GetText(sharedStrings) ?? string.Empty;
                // depending on how multi-line text is pasted into Excel, \r\n might be translated into _x000D_\n, 
                // because Excel internally only use \n as line delimiter.
                text = text.Replace("_x000D_\n", "\n");

                yield return text;

                columnIndex += 1;
            }
        }

        [ItemNotNull]
        [CanBeNull]
        private static IEnumerable<string> GetLanguageColumnHeaders([NotNull] this CultureKey language, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(language != null);

            var cultureKeyName = language.ToString();

            if ((scope == null) || scope.Comments.Contains(language))
                yield return CommentHeaderPrefix + cultureKeyName;

            if ((scope == null) || scope.Languages.Contains(language))
                yield return cultureKeyName;
        }

        [ItemNotNull]
        [CanBeNull]
        private static IEnumerable<string> GetLanguageDataColumns([NotNull] this ResourceTableEntry entry, [NotNull] CultureKey language, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(entry != null);
            Contract.Requires(language != null);

            if ((scope == null) || scope.Comments.Contains(language))
                yield return entry.Comments.GetValue(language);

            if ((scope == null) || scope.Languages.Contains(language))
                yield return entry.Values.GetValue(language);
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages">The languages.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>
        /// The header line.
        /// </returns>
        [NotNull, ItemNotNull]
        private static IEnumerable<IEnumerable<string>> GetHeaderRows([NotNull][ItemNotNull] this IEnumerable<CultureKey> languages, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<string>>>() != null);

            var languageColumnHeaders = languages.GetLanguageColumnHeaders(scope);

            yield return _fixedColumnHeaders.Concat(languageColumnHeaders);
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> GetLanguageColumnHeaders([NotNull][ItemNotNull] this IEnumerable<CultureKey> languages, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            return languages.SelectMany(lang => lang.GetLanguageColumnHeaders(scope));
        }

        /// <summary>
        /// Gets the text tables data lines.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="languages">The resource languages.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>
        /// The data lines.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        private static IEnumerable<IEnumerable<string>> GetDataRows([NotNull] this ResourceEntity entity, [NotNull][ItemNotNull] IEnumerable<CultureKey> languages, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(entity != null);
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<string>>>() != null);

            var entries = scope?.Entries.Where(entry => entry.Container == entity) ?? entity.Entries;

            return entries.Select(entry => entry.GetDataRow(languages, scope));
        }

        /// <summary>
        /// Gets one text tables line as an array of columns.
        /// </summary>
        /// <param name="entry">The entry for which to generate the line.</param>
        /// <param name="languages">The resource languages.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>
        /// The columns of this line.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> GetDataRow([NotNull] this ResourceTableEntry entry, [NotNull][ItemNotNull] IEnumerable<CultureKey> languages, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            return new[] { entry.Key }.Concat(entry.GetLanguageDataColumns(languages, scope));
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> GetLanguageDataColumns([NotNull] this ResourceTableEntry entry, [NotNull][ItemNotNull] IEnumerable<CultureKey> languages, [CanBeNull] IResourceScope scope)
        {
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            return languages.SelectMany(l => entry.GetLanguageDataColumns(l, scope));
        }

        [CanBeNull]
        public static TContainer AppendItem<TContainer, TItem>([CanBeNull] this TContainer container, [CanBeNull] TItem item)
            where TContainer : OpenXmlElement
            where TItem : OpenXmlElement
        {
            if ((container != null) && (item != null))
                // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                container.Append(item);

            return container;
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<MultipleSheetEntity> GetMultipleSheetEntities([NotNull] ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);
            Contract.Ensures(Contract.Result<IEnumerable<MultipleSheetEntity>>() != null);

            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return resourceManager.ResourceEntities
                .OrderBy(entity => entity.ProjectName)
                .ThenBy(entity => entity.BaseName)
                .Select((entity, index) => new MultipleSheetEntity(entity, index, uniqueNames));
        }

        private class MultipleSheetEntity
        {
            private const int MaxSheetNameLength = 31;
            [NotNull]
            private readonly ResourceEntity _resourceEntity;
            [CanBeNull]
            private readonly UInt32Value _sheetId;
            [NotNull]
            private readonly string _sheetName;

            public MultipleSheetEntity([NotNull] ResourceEntity resourceEntity, int index, [NotNull][ItemNotNull] ISet<string> uniqueNames)
            {
                Contract.Requires(resourceEntity != null);
                Contract.Requires(uniqueNames != null);

                _resourceEntity = resourceEntity;

                var sheetName = GetSheetName(resourceEntity, uniqueNames);
                _sheetName = sheetName;

                Id = "Id" + index + 1;
                _sheetId = UInt32Value.FromUInt32((uint)index + 1);
            }

            [NotNull]
            private static string GetSheetName([NotNull] ResourceEntity resourceEntity, [NotNull][ItemNotNull] ISet<string> uniqueNames)
            {
                Contract.Requires(resourceEntity != null);
                Contract.Requires(uniqueNames != null);
                Contract.Ensures(Contract.Result<string>() != null);

                var name = string.Join("|", resourceEntity.ProjectName, resourceEntity.BaseName);

                if ((name.Length > MaxSheetNameLength) || uniqueNames.Contains(name))
                {
                    name = Enumerable.Range(0, int.MaxValue)
                        .Select(i => GenrateShortName(i, name))
                        .FirstOrDefault(shortName => !uniqueNames.Contains(shortName));

                    if (name == null)
                        throw new InvalidOperationException("Failed to generate a unique short name.");
                }

                uniqueNames.Add(name);

                return name;
            }

            [NotNull]
            private static string GenrateShortName(int i, [NotNull] string name)
            {
                Contract.Requires(name != null);
                Contract.Ensures(Contract.Result<string>() != null);

                var suffix = "~" + i;
                Contract.Assume(suffix.Length < MaxSheetNameLength);
                var prefixLenght = Math.Min(name.Length, MaxSheetNameLength - suffix.Length);

                return name.Substring(0, prefixLenght) + suffix;
            }

            [NotNull]
            public string SheetName
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _sheetName;
                }
            }

            [NotNull]
            public ResourceEntity ResourceEntity
            {
                get
                {
                    Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                    return _resourceEntity;
                }
            }

            [CanBeNull]
            public string Id
            {
                get;
            }

            [NotNull]
            [ItemNotNull]
            public Sheet CreateSheet()
            {
                Contract.Ensures(Contract.Result<Sheet>() != null);

                return new Sheet
                {
                    Name = _sheetName,
                    SheetId = _sheetId,
                    Id = Id
                };
            }

            [NotNull]
            [ItemNotNull]
            public IEnumerable<IEnumerable<string>> GetDataRows([CanBeNull] IResourceScope scope)
            {
                Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<string>>>() != null);

                var languages = _resourceEntity.Languages
                    .Select(l => l.CultureKey)
                    .ToArray();

                return languages.GetHeaderRows(scope)
                    .Concat(_resourceEntity.GetDataRows(languages, scope));
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_resourceEntity != null);
                Contract.Invariant(_sheetName != null);
            }
        }

        private class FullScope : IResourceScope
        {
            public FullScope([NotNull][ItemNotNull] ICollection<ResourceEntity> entities)
            {
                Contract.Requires(entities != null);

                Entries = entities.SelectMany(entity => entity.Entries)
                    .ToArray();

                var languages = entities
                    .SelectMany(entity => entity.Languages)
                    .Select(l => l.CultureKey)
                    .Distinct()
                    .ToArray();

                Languages = languages;
                Comments = languages;
            }

            public IEnumerable<ResourceTableEntry> Entries
            {
                get;
            }

            public IEnumerable<CultureKey> Languages
            {
                get;
            }

            public IEnumerable<CultureKey> Comments
            {
                get;
            }
        }
    }
}