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

            if (!selection.Begin.EqualTo(selection.End))
                return true;

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

            var text = !selection.Begin.EqualTo(selection.End) ? selection.Text?.Trim('"') : parser.LocateString(selection);
            if (text == null)
                return null;

            var patterns = configuration.ParsePatterns().ToArray();

            var resourceManager = _exportProvider.GetExportedValue<ResourceManager>();

            resourceManager.Reload();

            var existingEntries = resourceManager.ResourceEntities
                .SelectMany(entity => entity.Entries)
                .Where(entry => entry.Values[null] == text)
                .ToArray();

            var viewModel = new MoveToResourceViewModel(_exportProvider, patterns, existingEntries, text)
            {
                SelectedResourceEntity = GetPreferredResourceEntity(document),
            };

            var confirmationDialog = new ConfirmationDialog(_exportProvider) { Content = viewModel };

            var confirmed = confirmationDialog.ShowDialog().GetValueOrDefault();

            if (confirmed && !string.IsNullOrEmpty(viewModel.Key))
            {
                ResourceTableEntry entry = null;

                if (!viewModel.ReuseExisiting)
                {
                    var entity = viewModel.SelectedResourceEntity;

                    entry = entity?.Add(viewModel.Key);
                    if (entry == null)
                        return null;

                    entry.Values[null] = viewModel.Value;
                    entry.Comment = viewModel.Comment;
                }

                selection.ReplaceWith(viewModel.Replacement);

                return entry;
            };

            return null;
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

            return new Selection(textDocument, line, codeLanguage);
        }

        private class Selection
        {
            private readonly TextDocument _textDocument;
            private readonly string _line;

            public Selection(TextDocument textDocument, string line, string codeLanguage)
            {
                Contract.Requires(textDocument != null);
                Contract.Requires(line != null);

                _textDocument = textDocument;
                _line = line;

                CodeLanguage = codeLanguage;
            }

            [ContractVerification(false)]
            public VirtualPoint Begin
            {
                get
                {
                    Contract.Ensures(Contract.Result<VirtualPoint>() != null);
                    return _textDocument.Selection.TopPoint;
                }
            }

            [ContractVerification(false)]
            public VirtualPoint End
            {
                get
                {
                    Contract.Ensures(Contract.Result<VirtualPoint>() != null);
                    return _textDocument.Selection.BottomPoint;
                }
            }

            [ContractVerification(false)]
            public string Text
            {
                get
                {
                    return _textDocument.Selection.Text;
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

                selection.MoveToLineAndOffset(Begin.Line, startColumn);
                selection.MoveToLineAndOffset(Begin.Line, endColumn, true);
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
                Contract.Invariant(_line != null);
            }
        }

        private interface IParser
        {
            string LocateString(Selection selection);
        }

        private class GenericParser : IParser
        {
            public string LocateString(Selection selection)
            {
                if (selection == null)
                    return null;

                var secondQuote = -1;
                var column = selection.Begin.LineCharOffset - 1;
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

                    if (column >= secondQuote)
                        continue;

                    var startIndex = firstQuote + 1;
                    var length = secondQuote - firstQuote - 1;
                    Contract.Assume(startIndex + length <= line.Length);

                    var value = line.Substring(startIndex, length);

                    var startColumn = firstQuote + 1;
                    var endColumn = secondQuote + 2;

                    selection.MoveTo(startColumn, endColumn);

                    return value;
                }

                return null;
            }
        }
    }
}
