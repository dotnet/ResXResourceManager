namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;

    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Core;

    public enum ExcelExportMode
    {
        [LocalizedDisplayName(StringResourceKey.ExcelExport_SingleSheet)]
        SingleSheet,
        [LocalizedDisplayName(StringResourceKey.ExcelExport_MultipleSheets)]
        MultipleSheets
    }

    public static partial class ResourceEntityExtensions
    {
        private static readonly string[] _singleSheetFixedColumnHeaders = { "Project", "File", "Key" };

        public static void ExportExcelFile(this ResourceManager resourceManager, string filePath, IResourceScope scope)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

            using (var package = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                Contract.Assume(package != null);

                var workbookPart = package.AddWorkbookPart();
                Contract.Assume(workbookPart != null);

                var exportMode = resourceManager.Configuration.ExcelExportMode;

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

        private static void ExportToMultipleSheets(ResourceManager resourceManager, WorkbookPart workbookPart, IResourceScope scope)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(workbookPart != null);

            var entitiesQuery = GetMultipleSheetEntities(resourceManager);

            if (scope != null)
            {
                var entitiesInScope = scope.Entries.Select(entry => entry.Owner).Distinct().ToArray();
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

        private static void ExportToSingleSheet(WorkbookPart workbookPart, IResourceScope scope)
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
            var dataRows = entries.Select(e => new[] { e.Owner.ProjectName, e.Owner.BaseName }.Concat(e.GetDataRow(languages, scope)));

            var rows = new[] { headerRow }.Concat(dataRows);

            worksheet.AppendItem(rows.Aggregate(new SheetData(), AppendRow));
        }

        public static void ImportExcelFile(this ResourceManager resourceManager, string filePath)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

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
                    return;

                var firstRow = firstSheet.GetRows(workbookPart).FirstOrDefault();

                if (IsSingleSheetHeader(firstRow, sharedStrings))
                {
                    ImportSingleSheet(resourceManager, firstSheet, workbookPart, sharedStrings);
                }
                else
                {
                    ImportMultipleSheets(resourceManager, sheets, workbookPart, sharedStrings);
                }
            }
        }

        private static void ImportSingleSheet(ResourceManager resourceManager, Sheet firstSheet, WorkbookPart workbookPart, IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(firstSheet != null);
            Contract.Requires(workbookPart != null);

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

                    var baseName = fileRows.Key;
                    if (string.IsNullOrEmpty(baseName))
                        continue;

                    var entity = resourceManager.ResourceEntities.FirstOrDefault(item => (item.ProjectName == projectName) && (item.BaseName == baseName));

                    if (entity == null)
                        continue;

                    var tableData = new[] { headerRow }.Concat(fileRows).ToArray();
                    Contract.Assume(tableData.Any());
                    entity.ImportTable(FixedColumnHeaders, tableData);
                }
            }
        }

        private static void ImportMultipleSheets(ResourceManager resourceManager, Sheets sheets, WorkbookPart workbookPart, IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(sheets != null);
            Contract.Requires(workbookPart != null);

            var entities = GetMultipleSheetEntities(resourceManager).ToArray();

            foreach (var sheet in sheets.OfType<Sheet>())
            {
                var resourceEntity = FindResourceEntity(entities, sheet);

                var rows = sheet.GetRows(workbookPart);

                var data = (IList<IList<string>>)rows.Select(row => row.GetCellValues(sharedStrings)).ToArray();
                if (data.Count == 0)
                    continue;

                resourceEntity.ImportTable(FixedColumnHeaders, data);
            }
        }

        private static bool IsSingleSheetHeader(Row firstRow, IList<SharedStringItem> sharedStrings)
        {
            return (firstRow != null) && firstRow.GetCellValues(sharedStrings)
                .Take(_singleSheetFixedColumnHeaders.Length)
                .SequenceEqual(_singleSheetFixedColumnHeaders);
        }

        private static IEnumerable<string> GetCellValues(this Row row, IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(row != null);

            return row.OfType<Cell>().GetCellValues(sharedStrings);
        }

        private static IEnumerable<Row> GetRows(this Sheet sheet, WorkbookPart workbookPart)
        {
            Contract.Requires(sheet != null);
            Contract.Requires(workbookPart != null);
            Contract.Ensures(Contract.Result<IEnumerable<Row>>() != null);

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);

            return worksheetPart.Maybe()
                .Select(x => x.Worksheet)
                .Select(x => x.ChildElements)
                .Select(x => x.OfType<SheetData>())
                .Select(x => x.FirstOrDefault())
                .Return(x => x.OfType<Row>()) ?? Enumerable.Empty<Row>();
        }

        private static ResourceEntity FindResourceEntity(this IEnumerable<MultipleSheetEntity> entities, Sheet sheet)
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
        private static string GetName(this Sheet sheet)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return sheet.Name.Value;
        }

        private static SheetData AppendRow(SheetData sheetData, IEnumerable<string> rowData)
        {
            Contract.Requires(sheetData != null);
            Contract.Requires(rowData != null);

            return sheetData.AppendItem(rowData.Aggregate(new Row(), (seed, item) => seed.AppendItem(CreateCell(item))));
        }

        private static Cell CreateCell(string text)
        {
            return new Cell { DataType = CellValues.InlineString }.AppendItem(new InlineString().AppendItem(new Text(text ?? string.Empty)));
        }

        private static IList<SharedStringItem> GetSharedStrings(this WorkbookPart workbookPart)
        {
            Contract.Requires(workbookPart != null);

            var sharedStringsPart = workbookPart.SharedStringTablePart;
            if (sharedStringsPart == null)
                return null;

            var stringTable = sharedStringsPart.SharedStringTable;

            if (stringTable == null)
                return null;

            return stringTable.OfType<SharedStringItem>().ToArray();
        }

        private static string GetText(this CellType cell, IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(cell != null);

            var cellValue = cell.CellValue;

            var dataType = cell.DataType;

            if (cellValue != null)
            {
                var text = cellValue.Text ?? string.Empty;

                if ((dataType != null) && (dataType == CellValues.SharedString))
                {
                    if (sharedStrings != null)
                    {
                        int index;
                        if (int.TryParse(text, out index) && (index >= 0) && (index < sharedStrings.Count))
                        {
                            var stringItem = sharedStrings[index];
                            if (stringItem != null)
                            {
                                var descendants = stringItem.Descendants<OpenXmlLeafTextElement>();
                                if (descendants != null)
                                {
                                    var content = descendants.Select(element => element.Text);
                                    text = string.Concat(content);
                                }
                            }
                        }
                    }
                }

                return text;
            }
            else
            {
                var descendants = cell.Descendants<OpenXmlLeafTextElement>();
                Contract.Assume(descendants != null);
                var content = descendants.Select(element => element.Text);
                var text = string.Concat(content);
                return text;
            }
        }

        private static IEnumerable<string> GetCellValues(this IEnumerable<Cell> cells, IList<SharedStringItem> sharedStrings)
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

                yield return cell.GetText(sharedStrings);
                columnIndex += 1;
            }
        }

        private static IEnumerable<string> GetLanguageColumnHeaders(this CultureKey language, IResourceScope scope)
        {
            Contract.Requires(language != null);

            var cultureKeyName = language.ToString();

            if ((scope == null) || scope.Comments.Contains(language))
                yield return CommentHeaderPrefix + cultureKeyName;

            if ((scope == null) || scope.Languages.Contains(language))
                yield return cultureKeyName;
        }

        private static IEnumerable<string> GetLanguageDataColumns(this ResourceTableEntry entry, CultureKey language, IResourceScope scope)
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
        private static IEnumerable<IEnumerable<string>> GetHeaderRows(this IEnumerable<CultureKey> languages, IResourceScope scope)
        {
            Contract.Requires(languages != null);

            var languageColumnHeaders = languages.GetLanguageColumnHeaders(scope);

            yield return FixedColumnHeaders.Concat(languageColumnHeaders);
        }

        private static IEnumerable<string> GetLanguageColumnHeaders(this IEnumerable<CultureKey> languages, IResourceScope scope)
        {
            Contract.Requires(languages != null);
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
        private static IEnumerable<IEnumerable<string>> GetDataRows(this ResourceEntity entity, IEnumerable<CultureKey> languages, IResourceScope scope)
        {
            Contract.Requires(entity != null);
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<string>>>() != null);

            var entries = (scope != null)
                ? scope.Entries.Where(entry => entry.Owner == entity)
                : entity.Entries;

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
        private static IEnumerable<string> GetDataRow(this ResourceTableEntry entry, IEnumerable<CultureKey> languages, IResourceScope scope)
        {
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);

            return (new[] { entry.Key }).Concat(entry.GetLanguageDataColumns(languages, scope));
        }

        private static IEnumerable<string> GetLanguageDataColumns(this ResourceTableEntry entry, IEnumerable<CultureKey> languages, IResourceScope scope)
        {
            Contract.Requires(entry != null);
            Contract.Requires(languages != null);

            return languages.SelectMany(l => entry.GetLanguageDataColumns(l, scope));
        }

        public static TContainer AppendItem<TContainer, TItem>(this TContainer container, TItem item)
            where TContainer : OpenXmlElement
            where TItem : OpenXmlElement
        {
            container.Append(item);
            return container;
        }

        private static IEnumerable<MultipleSheetEntity> GetMultipleSheetEntities(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);
            Contract.Ensures(Contract.Result<IEnumerable<MultipleSheetEntity>>() != null);

            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return resourceManager.ResourceEntities
                .OrderBy(entity => entity.ProjectName)
                .ThenBy(entity => entity.BaseName)
                .Select((entity, index) => new MultipleSheetEntity(entity, index, uniqueNames));
        }

        class MultipleSheetEntity
        {
            private const int MaxSheetNameLength = 31;
            private readonly ResourceEntity _resourceEntity;
            private readonly UInt32Value _sheetId;
            private readonly string _sheetName;

            public MultipleSheetEntity(ResourceEntity resourceEntity, int index, ISet<string> uniqueNames)
            {
                Contract.Requires(resourceEntity != null);
                Contract.Requires(uniqueNames != null);

                _resourceEntity = resourceEntity;

                var sheetName = GetSheetName(resourceEntity, uniqueNames);
                _sheetName = sheetName;

                Id = "Id" + index + 1;
                _sheetId = UInt32Value.FromUInt32((uint)index + 1);
            }

            private static string GetSheetName(ResourceEntity resourceEntity, ISet<string> uniqueNames)
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

            private static string GenrateShortName(int i, string name)
            {
                Contract.Requires(name != null);
                Contract.Ensures(Contract.Result<string>() != null);

                var suffix = "~" + i;
                Contract.Assume(suffix.Length < MaxSheetNameLength);
                var prefixLenght = Math.Min(name.Length, MaxSheetNameLength - suffix.Length);

                return name.Substring(0, prefixLenght) + suffix;
            }

            public string SheetName
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _sheetName;
                }
            }

            public ResourceEntity ResourceEntity
            {
                get
                {
                    Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                    return _resourceEntity;
                }
            }

            public string Id
            {
                get;
                private set;
            }

            public Sheet CreateSheet()
            {
                return new Sheet
                {
                    Name = _sheetName,
                    SheetId = _sheetId,
                    Id = Id
                };
            }

            public IEnumerable<IEnumerable<string>> GetDataRows(IResourceScope scope)
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
            private void ObjectInvariant()
            {
                Contract.Invariant(_resourceEntity != null);
                Contract.Invariant(_sheetName != null);
            }
        }

        class FullScope : IResourceScope
        {
            public FullScope(ICollection<ResourceEntity> entities)
            {
                Contract.Requires(entities != null);

                Entries = entities.SelectMany(entity => entity.Entries)
                    .ToArray();

                var languages = entities.SelectMany(entity => entity.Languages.Select(l => l.CultureKey))
                    .Distinct()
                    .ToArray();

                Languages = languages;
                Comments = languages;
            }

            public IEnumerable<ResourceTableEntry> Entries
            {
                get;
                private set;
            }

            public IEnumerable<CultureKey> Languages
            {
                get;
                private set;
            }

            public IEnumerable<CultureKey> Comments
            {
                get;
                private set;
            }
        }
    }
}