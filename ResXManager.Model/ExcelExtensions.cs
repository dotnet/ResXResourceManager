namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;

    public static partial class ResourceEntityExtensions
    {
        public static void ExportExcel(this ResourceManager resourceManager, string filePath, IEnumerable<ResourceEntity> selectedEntities)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

            using (var package = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = package.AddWorkbookPart();

                var entities = GetEntities(resourceManager, selectedEntities);

                workbookPart.Workbook = new Workbook().AppendItem(entities.Aggregate(new Sheets(), (seed, item) => seed.AppendItem(item.CreateSheet())));

                foreach (var item in entities)
                {
                    Contract.Assume(item != null);

                    workbookPart.AddNewPart<WorksheetPart>(item.Id).Worksheet = new Worksheet().AppendItem(item.GetDataRows().Aggregate(new SheetData(), AppendRow));
                }
            }
        }

        public static void ImportExcel(this ResourceManager resourceManager, string filePath)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

            switch (resourceManager.InternalImportExcel(filePath))
            {
                case ImportResult.Partial:
                    MessageBox.Show(Properties.Resources.ImportFailedPartiallyError, Properties.Resources.Title);
                    break;

                case ImportResult.None:
                    MessageBox.Show(Properties.Resources.ImportFailedError, Properties.Resources.Title);
                    break;

                case ImportResult.All:
                    break;

                default:
                    throw new InvalidOperationException(@"Undefined import result.");

            }
        }

        private static ImportResult InternalImportExcel(this ResourceManager resourceManager, string filePath)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(filePath != null);

            using (var package = SpreadsheetDocument.Open(filePath, false))
            {
                var workbookPart = package.WorkbookPart;

                var workbook = workbookPart.Workbook;
                IList<SharedStringItem> sharedStrings = workbookPart.GetSharedStrings();

                var entities = GetEntities(resourceManager, null);

                foreach (Sheet sheet in workbook.Sheets)
                {
                    var resourceEntity = FindResourceEntity(entities, sheet);

                    if (resourceEntity == null)
                        return ImportResult.Partial;

                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    var sheetData = worksheetPart.Worksheet.ChildElements.OfType<SheetData>().FirstOrDefault();

                    if (sheetData == null)
                        continue;

                    var rows = sheetData.OfType<Row>().ToArray();

                    var data = (IList<IList<string>>)rows.Select(row => row.OfType<Cell>().GetRowContent(sharedStrings)).ToArray();

                    var result = resourceEntity.ImportTable(FixedColumnHeaders, data);

                    switch (result)
                    {
                        case ImportResult.None:
                        case ImportResult.Partial:
                            return result;
                    }
                }
            }

            return ImportResult.All;
        }

        private static ResourceEntity FindResourceEntity(this IEnumerable<Entity> entities, Sheet sheet)
        {
            Contract.Requires(entities != null);
            Contract.Requires(sheet != null);

            var name = GetName(sheet);

            return entities.Where(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.ResourceEntity)
                .FirstOrDefault();
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
                                var content = stringItem.Descendants<OpenXmlLeafTextElement>().Select(element => element.Text);
                                text = string.Concat(content);
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

        private static IList<string> GetRowContent(this IEnumerable<Cell> cells, IList<SharedStringItem> sharedStrings)
        {
            Contract.Requires(cells != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);

            var result = new List<string>();

            foreach (var cell in cells)
            {
                var columnIndex = new ExcelRange(cell.CellReference).StartColumnIndex;

                while (result.Count < columnIndex)
                    result.Add(string.Empty);

                result.Add(cell.GetText(sharedStrings));
            }

            return result;
        }

        private static IEnumerable<string> GetLanguageColumnHeaders(string language)
        {
            yield return CommentHeaderPrefix + language;
            yield return language;
        }

        private static IEnumerable<string> GetLanguageDataColumns(this ResourceTableEntry entry, string language)
        {
            yield return entry.Comments[language];
            yield return entry.Values[language];
        }

        /// <summary>
        ///     Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The header line.</returns>
        public static IEnumerable<IEnumerable<string>> GetHeaderRows(this ResourceEntity entity)
        {
            Contract.Requires(entity != null);

            var languageColumns = entity.Languages.SelectMany(l => GetLanguageColumnHeaders(l.Name));

            yield return FixedColumnHeaders.Concat(languageColumns);
        }

        /// <summary>
        ///     Gets the text tables data lines.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The data lines.</returns>
        public static IEnumerable<IEnumerable<string>> GetDataRows(this ResourceEntity entity)
        {
            Contract.Requires(entity != null);

            return entity.Entries.Select(entity.GetDataRow);
        }

        /// <summary>
        ///     Gets one text tables line as an array of columns.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entry">The entry for which to generate the line.</param>
        /// <returns>The columns of this line.</returns>
        private static IEnumerable<string> GetDataRow(this ResourceEntity entity, ResourceTableEntry entry)
        {
            Contract.Requires(entity != null);
            Contract.Requires(entry != null);

            return (new[] { entry.Key }).Concat(entity.Languages.SelectMany(l => entry.GetLanguageDataColumns(l.Name)));
        }

        public static TContainer AppendItem<TContainer, TItem>(this TContainer container, TItem item)
            where TContainer : OpenXmlElement
            where TItem : OpenXmlElement
        {
            container.Append(item);
            return container;
        }

        private static ICollection<Entity> GetEntities(ResourceManager resourceManager, IEnumerable<ResourceEntity> selectedEntities)
        {
            Contract.Requires(resourceManager != null);
            Contract.Ensures(Contract.Result<ICollection<Entity>>() != null);

            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return resourceManager.ResourceEntities
                .OrderBy(entity => entity.ProjectName)
                .ThenBy(entity => entity.BaseName)
                .Select((entity, index) => new Entity(entity, index, uniqueNames))
                .Where(entity => (selectedEntities == null) || selectedEntities.Contains(entity.ResourceEntity))
                .ToArray();
        }

        class Entity
        {
            private const int MaxSheetNameLength = 31;
            private readonly ResourceEntity _resourceEntity;
            private readonly UInt32Value _sheetId;
            private readonly string _name;

            public Entity(ResourceEntity resourceEntity, int index, ISet<string> uniqueNames)
            {
                Contract.Requires(resourceEntity != null);
                Contract.Requires(uniqueNames != null);

                _resourceEntity = resourceEntity;

                var name = GetSheetName(resourceEntity, uniqueNames);

                _name = name;

                Id = "Id" + index + 1;
                _sheetId = UInt32Value.FromUInt32((uint)index + 1);
            }

            private static string GetSheetName(ResourceEntity resourceEntity, ISet<string> uniqueNames)
            {
                Contract.Requires(resourceEntity != null);
                Contract.Requires(uniqueNames != null);

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

            public string Name
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _name;
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
                    Name = _name,
                    SheetId = _sheetId,
                    Id = Id
                };
            }

            public IEnumerable<IEnumerable<string>> GetDataRows()
            {
                Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<string>>>() != null);

                return _resourceEntity.GetHeaderRows().Concat(_resourceEntity.GetDataRows());
            }

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_resourceEntity != null);
                Contract.Invariant(_name != null);
            }
        }
    }
}