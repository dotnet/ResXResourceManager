namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    using EnvDTE;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.VSIX.Visuals;

    public interface IRefactorings
    {
        bool CanMoveToResource(Document document);

        ResourceTableEntry MoveToResource(Document document);
    }

    [Export(typeof(IRefactorings))]
    internal class Refactorings : IRefactorings
    {
        private readonly ExportProvider _exportProvider;

        [ImportingConstructor]
        public Refactorings(ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);

            _exportProvider = exportProvider;
        }

        public bool CanMoveToResource(Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return false;

            var configuration = _exportProvider.GetExportedValue<DteConfiguration>();

            var configurations = configuration.MoveToResources.Items
                .Where(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (!configurations.Any())
                return false;

            Contract.Assume(document != null);
            var selection = GetSelection(document);
            if (selection == null)
                return false;

            IParser parser = new GenericParser();

            var s = parser.LocateString(selection);

            return s != null;
        }

        public ResourceTableEntry MoveToResource(Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return null;

            var configuration = _exportProvider.GetExportedValue<DteConfiguration>().MoveToResources.Items
                .FirstOrDefault(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (configuration == null)
                return null;

            Contract.Assume(document != null);
            var selection = GetSelection(document);
            if (selection == null)
                return null;

            IParser parser = new GenericParser();

            var stringInfo = parser.LocateString(selection);

            if (stringInfo == null)
                return null;

            selection.MoveTo(stringInfo.StartColumn, stringInfo.EndColumn);
            var patterns = configuration.ParsePatterns().ToArray();

            var viewModel = new MoveToResourceViewModel(_exportProvider, patterns)
            {
                SelectedResourceEntity = GetPreferredResourceEntity(document),
                Key = CreateKey(stringInfo.Value),
                Value = stringInfo.Value,
                Comment = ""
            };

            var confirmed = new ConfirmationDialog(_exportProvider) { Content = viewModel }.ShowDialog().GetValueOrDefault();
            if (confirmed && !string.IsNullOrEmpty(viewModel.Key))
            {
                var entity = viewModel.SelectedResourceEntity;

                var entry = entity?.Add(viewModel.Key);
                if (entry == null)
                    return null;

                entry.Values[null] = viewModel.Value;
                entry.Comments[null] = viewModel.Comment;

                selection.ReplaceWith(viewModel.Replacement);

                return entry;
            };

            return null;
        }

        private static string CreateKey(string value)
        {
            value = value?.Aggregate(new StringBuilder(), (builder, c) => builder.Append(IsCharValidForSymbol(c) ? c : '_'))?.ToString() ?? "_";

            if (!IsCharValidForSymbolStart(value.FirstOrDefault()))
                value = "_" + value;

            return value;
        }

        private ResourceEntity GetPreferredResourceEntity(Document document)
        {
            Contract.Requires(document != null);

            try
            {
                var resourceManager = _exportProvider.GetExportedValue<ResourceManager>();

                var project = document.ProjectItem?.ContainingProject;

                return resourceManager.ResourceEntities.FirstOrDefault(entity => IsInProject(entity, project));
            }
            catch (ExternalException)
            {
                return null;
            }
        }

        private static bool IsInProject(ResourceEntity entity, Project project)
        {
            Contract.Requires(entity != null);

            return ((DteProjectFile)entity.NeutralProjectFile)?.ProjectItems.Select(item => item.ContainingProject).Contains(project) ?? false;
        }

        private static Selection GetSelection(Document document)
        {
            Contract.Requires(document != null);

            var textDocument = (TextDocument)document.Object("TextDocument");

            var topPoint = textDocument?.Selection?.TopPoint;
            if (topPoint == null)
                return null;

            var line = textDocument.CreateEditPoint()?.GetLines(topPoint.Line, topPoint.Line + 1);
            if (line == null)
                return null;

            var codeLanguage = document.ProjectItem?.FileCodeModel?.Language;

            return new Selection(textDocument, topPoint, line, codeLanguage);
        }

        private static bool IsCharValidForSymbol(char c)
        {
            return (c == '_') || char.IsLetterOrDigit(c);
        }
        private static bool IsCharValidForSymbolStart(char c)
        {
            return (c == '_') || char.IsLetter(c);
        }

        private class Selection
        {
            private readonly TextDocument _textDocument;
            private readonly VirtualPoint _point;
            private readonly string _line;

            public Selection(TextDocument textDocument, VirtualPoint point, string line, string codeLanguage)
            {
                Contract.Requires(textDocument != null);
                Contract.Requires(point != null);
                Contract.Requires(line != null);

                _textDocument = textDocument;
                _point = point;
                _line = line;

                CodeLanguage = codeLanguage;
            }

            public VirtualPoint Point
            {
                get
                {
                    Contract.Ensures(Contract.Result<VirtualPoint>() != null);
                    return _point;
                }
            }

            public string Line
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _line;
                }
            }

            public string CodeLanguage { get; }

            public void MoveTo(int startColumn, int endColumn)
            {
                var selection = _textDocument.Selection;
                if (selection == null)
                    return;

                selection.MoveToLineAndOffset(Point.Line, startColumn);
                selection.MoveToLineAndOffset(Point.Line, endColumn, true);
            }

            public void ReplaceWith(string replacement)
            {
                var selection = _textDocument.Selection;
                if (selection == null)
                    return;

                selection.Text = replacement;
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_textDocument != null);
                Contract.Invariant(_point != null);
                Contract.Invariant(_line != null);
            }
        }

        private class StringInfo
        {
            private readonly string _value;

            public StringInfo(int startColumn, int endColumn, string value)
            {
                Contract.Requires(value != null);

                StartColumn = startColumn;
                EndColumn = endColumn;
                _value = value;
            }

            public int StartColumn { get; }

            public int EndColumn { get; }

            public string Value
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _value;
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_value != null);
            }
        }

        private interface IParser
        {
            StringInfo LocateString(Selection selection);
        }

        private class GenericParser : IParser
        {
            public StringInfo LocateString(Selection selection)
            {
                Contract.Requires(selection != null);

                var secondQuote = -1;
                var column = selection.Point.LineCharOffset - 1;
                var line = selection.Line;
                if (string.IsNullOrEmpty(line))
                    return null;

                while (secondQuote < line.Length)
                {
                    var firstQuote = line.IndexOf("\"", secondQuote + 1, StringComparison.Ordinal);
                    if (firstQuote == -1)
                        return null;

                    if (line.Length <= firstQuote + 1)
                        return null;

                    if (column < firstQuote)
                        return null;

                    secondQuote = line.IndexOf("\"", firstQuote + 1, StringComparison.Ordinal);
                    if (secondQuote == -1)
                        return null;

                    if (column < secondQuote)
                    {
                        var startIndex = firstQuote + 1;
                        var length = secondQuote - firstQuote - 1;
                        Contract.Assume(startIndex + length <= line.Length);

                        var value = line.Substring(startIndex, length);

                        return new StringInfo(firstQuote + 1, secondQuote + 2, value);
                    }
                }

                return null;
            }
        }
    }
}
