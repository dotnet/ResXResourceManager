namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;

    using EnvDTE;

    using JetBrains.Annotations;

    using ResXManager.Model;
    using ResXManager.View.Behaviors;
    using ResXManager.View.Properties;
    using ResXManager.View.Visuals;
    using ResXManager.VSIX.Visuals;

    using TomsToolbox.Composition;

    public interface IRefactorings
    {
        bool CanMoveToResource(Document? document);

        ResourceTableEntry? MoveToResource(Document? document);
    }

    [Export(typeof(IRefactorings))]
    internal class Refactorings : IRefactorings
    {
        [NotNull]
        private readonly IExportProvider _exportProvider;
        private ResourceEntity? _lastUsedEntity;
        private bool _isLastUsedEntityInSameProject;

        [ImportingConstructor]
        public Refactorings([NotNull] IExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
        }

        public bool CanMoveToResource(Document? document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return false;

            var configuration = _exportProvider.GetExportedValue<DteConfiguration>();

            var configurations = configuration.MoveToResources.Items
                .Where(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (!configurations.Any())
                return false;

            var selection = GetSelection(document);
            if (selection == null)
                return false;

            if (!selection.Begin.EqualTo(selection.End))
                return true;

            IParser parser = new GenericParser();

            var s = parser.LocateString(selection, false);

            return s != null;
        }

        public ResourceTableEntry? MoveToResource(Document? document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return null;

            var fileName = Path.GetFileNameWithoutExtension(document.FullName);

            var configurationItems = _exportProvider.GetExportedValue<DteConfiguration>().MoveToResources.Items;

            var configuration = configurationItems
                .FirstOrDefault(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (configuration == null)
                return null;

            var selection = GetSelection(document);
            if (selection == null)
                return null;

            IParser parser = new GenericParser();

            var text = !selection.IsEmpty ? selection.Text?.Trim('"', '\'', '`') : parser.LocateString(selection, true);
            if (string.IsNullOrEmpty(text))
                return null;

            var patterns = configuration.ParsePatterns().ToArray();

            var resourceViewModel = _exportProvider.GetExportedValue<ResourceViewModel>();

            resourceViewModel.Reload();

            var resourceManager = _exportProvider.GetExportedValue<ResourceManager>();

            var entities = resourceManager.ResourceEntities
                .Where(entity => !entity.IsWinFormsDesignerResource)
                .ToList();

            var filter = EntityFilter.BuildFilter(Settings.Default.ResourceFilter);
            if (filter != null)
            {
                entities.RemoveAll(item => !filter(item.ToString()));
            }

            var projectResources = new HashSet<ResourceEntity>(GetResourceEntiesFromProject(document, entities));

            // put resources from the same project on top
            entities.RemoveAll(entity => projectResources.Contains(entity));
            entities.InsertRange(0, projectResources);

            // put the last used entry on top, if it's in the same project, or the last access was cross-project.
            if (_lastUsedEntity != null)
            {
                if (!_isLastUsedEntityInSameProject || IsInProject(_lastUsedEntity, document))
                {
                    entities.Remove(_lastUsedEntity);
                    entities.Insert(0, _lastUsedEntity);
                }
            }

            var viewModel = new MoveToResourceViewModel(patterns, entities, text, extension, selection.ClassName, selection.FunctionName, fileName);

            var confirmed = ConfirmationDialog.Show(_exportProvider, viewModel, Resources.MoveToResource, null).GetValueOrDefault();

            if (!confirmed || string.IsNullOrEmpty(viewModel.Key))
                return null;

            ResourceTableEntry entry = null;

            if (!viewModel.ReuseExisiting)
            {
                var entity = _lastUsedEntity = viewModel.SelectedResourceEntity;
                if (entity == null)
                    return null;

                _isLastUsedEntityInSameProject = IsInProject(entity, document);

                entry = entity.Add(viewModel.Key);
                if (entry == null)
                    return null;

                entry.Values[null] = viewModel.Value;
                entry.Comment = viewModel.Comment;
            }

            selection.ReplaceWith(viewModel.Replacement?.Value);

            return entry;
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<ResourceEntity> GetResourceEntiesFromProject([NotNull] Document document, [NotNull][ItemNotNull] IEnumerable<ResourceEntity> entities)
        {
            try
            {
                var project = document.ProjectItem?.ContainingProject;

                return entities.Where(entity => IsInProject(entity, project));
            }
            catch (Exception)
            {
                return Enumerable.Empty<ResourceEntity>();
            }
        }

        private static bool IsInProject([NotNull] ResourceEntity entity, Document? document)
        {
            try
            {
                var project = document?.ProjectItem?.ContainingProject;

                return IsInProject(entity, project);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsInProject([NotNull] ResourceEntity entity, Project? project)
        {
            var projectFile = entity.NeutralProjectFile as DteProjectFile;

            return projectFile?.ProjectItems
                .Select(item => item.ContainingProject)
                .Contains(project) ?? false;
        }

        private static Selection? GetSelection(Document? document)
        {
            var textDocument = (TextDocument?)document?.Object(@"TextDocument");

            var topPoint = textDocument?.Selection?.TopPoint;
            if (topPoint == null)
                return null;

            var line = textDocument.CreateEditPoint()?.GetLines(topPoint.Line, topPoint.Line + 1);
            if (line == null)
                return null;

            var fileCodeModel = document.ProjectItem?.FileCodeModel;

            return new Selection(textDocument, line, fileCodeModel);
        }

        private class Selection
        {
            [NotNull]
            private readonly TextDocument _textDocument;

            private readonly FileCodeModel? _codeModel;

            public Selection([NotNull] TextDocument textDocument, [NotNull] string line, FileCodeModel? codeModel)
            {
                _textDocument = textDocument;
                Line = line;
                _codeModel = codeModel;
            }

            [NotNull]
            public VirtualPoint Begin => _textDocument.Selection.TopPoint;

            [NotNull]
            public VirtualPoint End => _textDocument.Selection.BottomPoint;

            public bool IsEmpty => Begin.EqualTo(End);

            public string? Text => _textDocument.Selection?.Text;

            [NotNull]
            public string Line { get; }

            public string? FunctionName => GetCodeElement(vsCMElement.vsCMElementFunction)?.Name;

            public string? ClassName => GetCodeElement(vsCMElement.vsCMElementClass)?.Name;

            public void MoveTo(int startColumn, int endColumn)
            {
                var selection = _textDocument.Selection;
                if (selection == null)
                    return;

                selection.MoveToLineAndOffset(Begin.Line, startColumn);
                selection.MoveToLineAndOffset(Begin.Line, endColumn, true);
            }

            public void ReplaceWith(string? replacement)
            {
                var selection = _textDocument.Selection;
                // using "selection.Text = replacement" does not work here, since it will trigger auto-complete,
                // and this may add unwanted additional characters, resulting in bad code.
                selection?.ReplaceText(selection.Text, replacement);
            }

            private CodeElement? GetCodeElement(vsCMElement scope)
            {
                try
                {
                    return _codeModel?.CodeElementFromPoint(Begin, scope);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private interface IParser
        {
            string? LocateString(Selection? selection, bool expandSelection);
        }

        private class GenericParser : IParser
        {
            public string? LocateString(Selection? selection, bool expandSelection)
            {
                if (selection == null)
                    return null;

                var column = selection.Begin.LineCharOffset - 1;
                var line = selection.Line;
                if (string.IsNullOrEmpty(line))
                    return null;

                if (!expandSelection)
                    selection = null;

                var locator = new Locator(line, column, selection);

                return locator.Locate(@"""") ?? locator.Locate("'") ?? locator.Locate("`");
            }

            private class Locator
            {
                private readonly string _line;
                private readonly int _column;
                private readonly Selection? _selection;

                public Locator(string line, int column, Selection? selection)
                {
                    _line = line;
                    _column = column;
                    _selection = selection;
                }

                public string? Locate(string quote)
                {
                    var secondQuote = -1;

                    while (secondQuote < _line.Length)
                    {
                        var firstQuote = _line.IndexOf(quote, secondQuote + 1, StringComparison.Ordinal);
                        if (firstQuote == -1)
                            return null;

                        if (_line.Length <= firstQuote + 1)
                            return null;

                        if (_column < firstQuote)
                            return null;

                        secondQuote = _line.IndexOf(quote, firstQuote + 1, StringComparison.Ordinal);
                        if (secondQuote == -1)
                            return null;

                        if (_column >= secondQuote)
                            continue;

                        var startIndex = firstQuote + 1;
                        var length = secondQuote - firstQuote - 1;

                        if (_selection != null)
                        {
                            var startColumn = firstQuote + 1;
                            var endColumn = secondQuote + 2;

                            _selection.MoveTo(startColumn, endColumn);
                        }

                        return _line.Substring(startIndex, length);
                    }

                    return null;
                }
            }
        }
    }
}
