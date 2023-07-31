namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;
    using TomsToolbox.Essentials;

    public enum ExcelExportMode
    {
        [LocalizedDisplayName(StringResourceKey.ExcelExport_SingleSheet)]
        SingleSheet,
        [LocalizedDisplayName(StringResourceKey.ExcelExport_MultipleSheets)]
        MultipleSheets,
        [LocalizedDisplayName(StringResourceKey.ExcelExport_PlainTextTabDelimited)]
        Text
    }

    public static partial class ResourceEntityExtensions
    {
        private static readonly string[] _singleSheetFixedColumnHeaders = { "Project", "File", "Key" };

        public static void ExportExcelFile(this ResourceManager resourceManager, string filePath, IResourceScope? scope, ExcelExportMode exportMode)
        {
            if (exportMode == ExcelExportMode.Text)
            {
                ExportToText(filePath, scope ?? new FullScope(resourceManager.ResourceEntities));
                return;
            }

            using (var package = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = package.AddWorkbookPart();

                GeneratedCode.GeneratedClass.AddStylesToWorkbookPart(workbookPart);

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

        private static void ExportToText(string filePath, IResourceScope scope)
        {
            var languages = scope.Languages.Concat(scope.Comments).Distinct().ToArray();
            var entries = scope.Entries.ToArray();

            var headerRow = (IList<string?>)_singleSheetFixedColumnHeaders.Concat(languages.GetLanguageColumnHeaders(scope)).ToArray();
            var dataRows = entries.Select(e => (IList<string?>)new[] { e.Container.ProjectName, e.Container.UniqueName }.Concat(e.GetDataRow(languages, scope)).ToArray());

            var rows = new[] { headerRow }.Concat(dataRows).ToArray();

            var data = rows.ToTextString();

            File.WriteAllText(filePath, data);
        }

        private static void ExportToMultipleSheets(ResourceManager resourceManager, WorkbookPart workbookPart, IResourceScope? scope)
        {
            var entitiesQuery = GetMultipleSheetEntities(resourceManager);

            if (scope != null)
            {
                var entitiesInScope = scope.Entries.Select(entry => entry.Container).Distinct().ToArray();
                entitiesQuery = entitiesQuery.Where(entity => entitiesInScope.Contains(entity.ResourceEntity));
            }

            var entities = entitiesQuery.ToArray();

            workbookPart.Workbook = new Workbook().AppendItem(entities.Aggregate(new Sheets(), (seed, item) => seed.AppendItem(item.CreateSheet())));

            var dataAppender = new DataAppender(_fixedColumnHeaders.Length);

            foreach (var item in entities)
            {
                workbookPart
                    .AddNewPart<WorksheetPart>(item.Id)
                    // ReSharper disable once AssignNullToNotNullAttribute
                    .Worksheet = new Worksheet()
                        .AppendItem(item.GetDataRows(scope).Aggregate(new SheetData(), dataAppender.AppendRow))
                        .Protect();
            }
        }

        private static void ExportToSingleSheet(WorkbookPart workbookPart, IResourceScope scope)
        {
            var languages = scope.Languages.Concat(scope.Comments).Distinct().ToArray();
            var entries = scope.Entries.ToArray();

            const string sheetId = "Id1";
            var sheet = new Sheet { Name = "ResXResourceManager", Id = sheetId, SheetId = 1 };

            var headerRow = _singleSheetFixedColumnHeaders.Concat(languages.GetLanguageColumnHeaders(scope));
            var dataRows = entries.Select(e => new[] { e.Container.ProjectName, e.Container.UniqueName }.Concat(e.GetDataRow(languages, scope)));
            var rows = new[] { headerRow }.Concat(dataRows);

            var dataAppender = new DataAppender(_singleSheetFixedColumnHeaders.Length);

            workbookPart.Workbook = new Workbook().AppendItem(new Sheets(sheet));

            var sheetData = rows.Aggregate(new SheetData(), dataAppender.AppendRow);
            if (sheetData.ChildElements.Count > 1048576)
            {
                throw new ImportException("The Excel limit is 1048576 rows per sheet. The data can't be exported");
            }

            workbookPart
                .AddNewPart<WorksheetPart>(sheetId)
                // ReSharper disable once AssignNullToNotNullAttribute
                .Worksheet = new Worksheet()
                    .AppendItem(sheetData)
                    .Protect();
        }

        private static Worksheet Protect(this Worksheet worksheet)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return worksheet.AppendItem(new SheetProtection { Sheet = true, Objects = true, Scenarios = true, FormatColumns = false, FormatRows = false, Sort = false, AutoFilter = false });
        }

        public static IList<EntryChange> ImportExcelFile(this ResourceManager resourceManager, string filePath)
        {
            using var spreadsheetStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(spreadsheetStream);
            var content = reader.ReadToEnd();

            if (content.StartsWith(string.Join("\t", _singleSheetFixedColumnHeaders), StringComparison.OrdinalIgnoreCase))
            {
                return ImportSingleSheet(resourceManager, content.ParseTable()).ToArray();
            }

            spreadsheetStream.Position = 0;
            using (var package = SpreadsheetDocument.Open(spreadsheetStream, false))
            {
                var workbookPart = package.WorkbookPart;
                var workbook = workbookPart?.Workbook;
                var sharedStrings = workbookPart?.GetSharedStrings();
                var sheets = workbook?.Sheets;
                if (sheets == null)
                    return Array.Empty<EntryChange>();

                var firstSheet = sheets.OfType<Sheet>().FirstOrDefault();
                if (firstSheet == null)
                    return Array.Empty<EntryChange>();

                var firstRow = firstSheet.GetRows(workbookPart).FirstOrDefault();

                var changes = IsSingleSheetHeader(firstRow, sharedStrings)
                    ? ImportSingleSheet(resourceManager, firstSheet, workbookPart, sharedStrings)
                    : ImportMultipleSheets(resourceManager, sheets, workbookPart, sharedStrings);

                return changes.ToArray();
            }
        }

        private static IEnumerable<EntryChange> ImportSingleSheet(ResourceManager resourceManager, Sheet firstSheet, WorkbookPart workbookPart, IList<SharedStringItem>? sharedStrings)
        {
            var data = firstSheet.GetRows(workbookPart).Select(row => row.GetCellValues(sharedStrings)).ToArray();

            return ImportSingleSheet(resourceManager, data);
        }

        private static IEnumerable<EntryChange> ImportSingleSheet(ResourceManager resourceManager, IList<IList<string>>? data)
        {
            var firstRow = data?.FirstOrDefault();
            if (firstRow == null)
                yield break;

            var headerRow = (IList<string>)firstRow.Skip(2).ToArray();

            var rowsByProject = data.Skip(1).GroupBy(row => row.FirstOrDefault() ?? string.Empty, row => (IList<string>)row.Skip(1).ToArray());

            foreach (var projectRows in rowsByProject)
            {
                var projectName = projectRows.Key;

                if (projectName.IsNullOrEmpty())
                    continue;

                var rowsByFile = projectRows.GroupBy(row => row.FirstOrDefault() ?? string.Empty, row => (IList<string>)row.Skip(1).ToArray());

                foreach (var fileRows in rowsByFile)
                {
                    var uniqueName = fileRows.Key;
                    if (uniqueName.IsNullOrEmpty())
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

        private static IEnumerable<EntryChange> ImportMultipleSheets(ResourceManager resourceManager, Sheets sheets, WorkbookPart workbookPart, IList<SharedStringItem>? sharedStrings)
        {
            var entities = GetMultipleSheetEntities(resourceManager).ToArray();

            var changes = sheets.OfType<Sheet>()
                .SelectMany(sheet => FindResourceEntity(entities, sheet).ImportTable(_fixedColumnHeaders, sheet.GetTable(workbookPart, sharedStrings)));

            return changes;
        }

        private static IList<string>[] GetTable(this Sheet sheet, WorkbookPart workbookPart, IList<SharedStringItem>? sharedStrings)
        {
            return sheet.GetRows(workbookPart)
                .Select(row => row.GetCellValues(sharedStrings))
                .ToArray();
        }

        private static bool IsSingleSheetHeader(Row? firstRow, IList<SharedStringItem>? sharedStrings)
        {
            return (firstRow != null) && firstRow.GetCellValues(sharedStrings)
                .Take(_singleSheetFixedColumnHeaders.Length)
                .SequenceEqual(_singleSheetFixedColumnHeaders);
        }

        private static IList<string> GetCellValues(this Row row, IList<SharedStringItem>? sharedStrings)
        {
            return row.OfType<Cell>().GetCellValues(sharedStrings).ToArray();
        }

        private static IEnumerable<Row> GetRows(this Sheet sheet, WorkbookPart workbookPart)
        {
            var sheetId = sheet.Id;
            if (string.IsNullOrEmpty(sheetId))
                return Enumerable.Empty<Row>();

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheetId);

            return worksheetPart.Worksheet.ChildElements.OfType<SheetData>().FirstOrDefault()?.OfType<Row>() ?? Enumerable.Empty<Row>();
        }

        private static ResourceEntity FindResourceEntity(this IEnumerable<MultipleSheetEntity> entities, Sheet sheet)
        {
            var name = sheet.Name?.Value;

            var entity = entities.Where(item => item.SheetName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.ResourceEntity)
                .FirstOrDefault();

            if (entity == null)
                throw new ImportException(string.Format(CultureInfo.CurrentCulture, Resources.ImportMapSheetError, name));

            return entity;
        }

        private sealed class DataAppender
        {
            private readonly int _numberOfFixedColumns;

            public DataAppender(int numberOfFixedColumns)
            {
                _numberOfFixedColumns = numberOfFixedColumns;
            }

            public SheetData AppendRow(SheetData sheetData, IEnumerable<string> rowData)
            {
                var row = (sheetData.ChildElements?.OfType<Row>()?.Count() ?? 0) + 1;
                var column = 1;
                return sheetData.AppendItem(rowData.Aggregate(new Row(), (seed, item) => seed.AppendItem(CreateCell(item, row, column++))));
            }

            private Cell CreateCell(string? text, int row, int column)
            {
                if (text?.Length > 32767)
                {
                    throw new ImportException("The Excel limit is 32767 characters per cell. This text can't be exported: " + text.Substring(0, 50) + "...");
                }

                var cell = new Cell
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

                if ((row <= 1) || (column <= _numberOfFixedColumns))
                {
                    cell.StyleIndex = 1; // locked
                }

                return cell;
            }

            private static string CreateCellReference(int row, int column)
            {
                return GetExcelColumnName(column) + row.ToString(CultureInfo.InvariantCulture);
            }

            // From: https://stackoverflow.com/questions/181596/how-to-convert-a-column-number-eg-127-into-an-excel-column-eg-aa
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
        }

        private static IList<SharedStringItem>? GetSharedStrings(this WorkbookPart workbookPart)
        {
            var sharedStringsPart = workbookPart.SharedStringTablePart;

            var stringTable = sharedStringsPart?.SharedStringTable;

            return stringTable?.OfType<SharedStringItem>().ToArray();
        }

        private static string? GetText(this CellType cell, IList<SharedStringItem>? sharedStrings)
        {
            var cellValue = cell.CellValue;
            var dataType = cell.DataType;

            if (cellValue == null)
                return GetTextFromTextElement(cell);

            var text = cellValue.Text;

            if ((dataType != null) && (dataType == CellValues.SharedString) && (sharedStrings != null) && int.TryParse(text, out var index) && (index >= 0) && (index < sharedStrings.Count))
            {
                var stringItem = sharedStrings[index];

                return GetTextFromTextElement(stringItem) ?? text;
            }

            return text;

        }

        private static string? GetTextFromTextElement(OpenXmlElement cell)
        {
            return cell.ChildElements
                       .OfType<Text>()
                       .Select(item => item.Text)
                       .FirstOrDefault() ??
                   cell.ChildElements
                       .OfType<InlineString>()
                       .Select(item => item.Text?.Text)
                       .FirstOrDefault() ??
                   cell.ChildElements
                       .OfType<Run>()
                       .Select(item => item.Text?.Text)
                       .Aggregate(default(string), (a, b) => a + b);
        }

        private static IEnumerable<string> GetCellValues(this IEnumerable<Cell> cells, IList<SharedStringItem>? sharedStrings)
        {
            var columnIndex = 0;

            foreach (var cell in cells)
            {
                var cellColumnIndex = new ExcelRange(cell.CellReference).StartColumnIndex;

                while (columnIndex < cellColumnIndex)
                {
                    yield return string.Empty;
                    columnIndex += 1;
                }

                var text = cell.GetText(sharedStrings) ?? string.Empty;
                // depending on how multi-line text is pasted into Excel, \r\n might be translated into _x000D_\n,
                // because Excel internally only use \n as line delimiter.
                text = text.Replace("_x000D_\n", "\n", StringComparison.Ordinal);

                yield return text;

                columnIndex += 1;
            }
        }

        private static IEnumerable<string>? GetLanguageColumnHeaders(this CultureKey language, IResourceScope? scope)
        {
            var cultureKeyName = language.ToString();

            if ((scope == null) || scope.Comments.Contains(language))
                yield return CommentHeaderPrefix + cultureKeyName;

            if ((scope == null) || scope.Languages.Contains(language))
                yield return cultureKeyName;
        }

        private static IEnumerable<string>? GetLanguageDataColumns(this ResourceTableEntry entry, CultureKey language, IResourceScope? scope)
        {
            if ((scope == null) || scope.Comments.Contains(language))
                yield return entry.Comments.GetValue(language) ?? string.Empty;

            if ((scope == null) || scope.Languages.Contains(language))
                yield return entry.Values.GetValue(language) ?? string.Empty;
        }

        /// <summary>
        /// Gets the text tables header line as an enumerable so we can use it with "Concat".
        /// </summary>
        /// <param name="languages">The languages.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>
        /// The header line.
        /// </returns>
        private static IEnumerable<IEnumerable<string>> GetHeaderRows(this IEnumerable<CultureKey> languages, IResourceScope? scope)
        {
            var languageColumnHeaders = languages.GetLanguageColumnHeaders(scope);

            yield return _fixedColumnHeaders.Concat(languageColumnHeaders);
        }

        private static IEnumerable<string> GetLanguageColumnHeaders(this IEnumerable<CultureKey> languages, IResourceScope? scope)
        {
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
        private static IEnumerable<IEnumerable<string>> GetDataRows(this ResourceEntity entity, IEnumerable<CultureKey> languages, IResourceScope? scope)
        {
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
        private static IEnumerable<string> GetDataRow(this ResourceTableEntry entry, IEnumerable<CultureKey> languages, IResourceScope? scope)
        {
            return new[] { entry.Key }.Concat(entry.GetLanguageDataColumns(languages, scope));
        }

        private static IEnumerable<string> GetLanguageDataColumns(this ResourceTableEntry entry, IEnumerable<CultureKey> languages, IResourceScope? scope)
        {
            return languages.SelectMany(l => entry.GetLanguageDataColumns(l, scope));
        }

        [return: NotNullIfNotNull("container")]
        public static TContainer? AppendItem<TContainer, TItem>(this TContainer? container, TItem? item)
            where TContainer : OpenXmlElement
            where TItem : OpenXmlElement
        {
            if ((container != null) && (item != null))
                // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                container.Append(item);

            return container;
        }

        private static IEnumerable<MultipleSheetEntity> GetMultipleSheetEntities(ResourceManager resourceManager)
        {
            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return resourceManager.ResourceEntities
                .OrderBy(entity => entity.ProjectName)
                .ThenBy(entity => entity.BaseName)
                .Select((entity, index) => new MultipleSheetEntity(entity, index, uniqueNames));
        }

        private sealed class MultipleSheetEntity
        {
            private const int MaxSheetNameLength = 31;

            private readonly UInt32Value _sheetId;

            public MultipleSheetEntity(ResourceEntity resourceEntity, int index, ISet<string> uniqueNames)
            {
                ResourceEntity = resourceEntity;

                var sheetName = GetSheetName(resourceEntity, uniqueNames);
                SheetName = sheetName;

                Id = "Id" + index + 1;
                _sheetId = UInt32Value.FromUInt32((uint)index + 1);
            }

            private static string GetSheetName(ResourceEntity resourceEntity, ISet<string> uniqueNames)
            {
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
                var suffix = "~" + i;
                var prefixLenght = Math.Min(name.Length, MaxSheetNameLength - suffix.Length);

                return name.Substring(0, prefixLenght) + suffix;
            }

            public string SheetName { get; }

            public ResourceEntity ResourceEntity { get; }

            public string Id { get; }

            public Sheet CreateSheet()
            {
                return new Sheet
                {
                    Name = SheetName,
                    SheetId = _sheetId,
                    Id = Id
                };
            }

            public IEnumerable<IEnumerable<string>> GetDataRows(IResourceScope? scope)
            {
                var languages = ResourceEntity.Languages
                    .Select(l => l.CultureKey)
                    .ToArray();

                return languages.GetHeaderRows(scope)
                    .Concat(ResourceEntity.GetDataRows(languages, scope));
            }
        }

        private sealed class FullScope : IResourceScope
        {
            public FullScope(ICollection<ResourceEntity> entities)
            {
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

    namespace GeneratedCode
    {
        // Generated from an empty document using the OpenXML SDK Productivity Tool
        public static class GeneratedClass
        {
            public static void AddStylesToWorkbookPart(WorkbookPart part)
            {
                var workbookStylesPart = part.AddNewPart<WorkbookStylesPart>("WorkbookStyles");

                var stylesheet1 = new Stylesheet { MCAttributes = new MarkupCompatibilityAttributes { Ignorable = "x14ac" } };
                stylesheet1.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
                stylesheet1.AddNamespaceDeclaration("x14ac", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac");

                var fonts1 = new Fonts { Count = 1U, KnownFonts = true };

                var font1 = new Font();
                var fontSize1 = new FontSize { Val = 11D };
                var color1 = new Color { Theme = 1U };
                var fontName1 = new FontName { Val = "Calibri" };
                var fontFamilyNumbering1 = new FontFamilyNumbering { Val = 2 };
                var fontScheme1 = new FontScheme { Val = FontSchemeValues.Minor };

                font1.Append(fontSize1);
                font1.Append(color1);
                font1.Append(fontName1);
                font1.Append(fontFamilyNumbering1);
                font1.Append(fontScheme1);

                fonts1.Append(font1);

                var fills1 = new Fills { Count = 2U };

                var fill1 = new Fill();
                var patternFill1 = new PatternFill { PatternType = PatternValues.None };

                fill1.Append(patternFill1);

                var fill2 = new Fill();
                var patternFill2 = new PatternFill { PatternType = PatternValues.Gray125 };

                fill2.Append(patternFill2);

                fills1.Append(fill1);
                fills1.Append(fill2);

                var borders1 = new Borders { Count = 1U };

                var border1 = new Border();
                var leftBorder1 = new LeftBorder();
                var rightBorder1 = new RightBorder();
                var topBorder1 = new TopBorder();
                var bottomBorder1 = new BottomBorder();
                var diagonalBorder1 = new DiagonalBorder();

                border1.Append(leftBorder1);
                border1.Append(rightBorder1);
                border1.Append(topBorder1);
                border1.Append(bottomBorder1);
                border1.Append(diagonalBorder1);

                borders1.Append(border1);

                var cellStyleFormats1 = new CellStyleFormats { Count = 1U };
                var cellStyleFormat = new CellFormat { NumberFormatId = 0U, FontId = 0U, FillId = 0U, BorderId = 0U };

                cellStyleFormats1.Append(cellStyleFormat);

                var cellFormats = new CellFormats { Count = 2U };

                // index 0 (default) => unlocked
                var cellFormat0 = new CellFormat { NumberFormatId = 0U, FontId = 0U, FillId = 0U, BorderId = 0U, FormatId = 0U, ApplyProtection = true }.AppendItem(new Protection { Locked = false });
                // index 1 => locked
                var cellFormat1 = new CellFormat { NumberFormatId = 0U, FontId = 0U, FillId = 0U, BorderId = 0U, FormatId = 0U };

                cellFormats.Append(cellFormat0);
                cellFormats.Append(cellFormat1);

                var cellStyles1 = new CellStyles { Count = 1U };
                var cellStyle1 = new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U };

                cellStyles1.Append(cellStyle1);

                stylesheet1.Append(fonts1);
                stylesheet1.Append(fills1);
                stylesheet1.Append(borders1);
                stylesheet1.Append(cellStyleFormats1);
                stylesheet1.Append(cellFormats);
                stylesheet1.Append(cellStyles1);

                workbookStylesPart.Stylesheet = stylesheet1;
            }
        }
    }
}