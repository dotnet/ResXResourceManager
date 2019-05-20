namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Behaviors;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;
    using tomenglertde.ResXManager.VSIX.Visuals;

    using TomsToolbox.Desktop.Composition;

    public interface IRefactorings
    {
        bool CanMoveToResource([CanBeNull] EnvDTE.Document document);

        [CanBeNull]
        ResourceTableEntry MoveToResource([CanBeNull] EnvDTE.Document document);
    }

    [Export(typeof(IRefactorings))]
    internal class Refactorings : IRefactorings
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost;
        [CanBeNull]
        private ResourceEntity _lastUsedEntity;
        private bool _isLastUsedEntityInSameProject;

        [ImportingConstructor]
        public Refactorings([NotNull] ICompositionHost compositionHost)
        {
            _compositionHost = compositionHost;
        }

        public bool CanMoveToResource([CanBeNull] EnvDTE.Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return false;

            var configuration = _compositionHost.GetExportedValue<DteConfiguration>();

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

        [CanBeNull]
        public ResourceTableEntry MoveToResource([CanBeNull] EnvDTE.Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return null;

            var fileName = Path.GetFileNameWithoutExtension(document.FullName);

            var configurationItems = _compositionHost.GetExportedValue<DteConfiguration>().MoveToResources.Items;

            var configuration = configurationItems
                .FirstOrDefault(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (configuration == null)
                return null;

            var selection = GetSelection(document);
            if (selection == null)
                return null;

            IParser parser = new GenericParser();

            var text = !selection.IsEmpty ? selection.Text?.Trim('"') : parser.LocateString(selection, true);
            if (string.IsNullOrEmpty(text))
                return null;

            var patterns = configuration.ParsePatterns().ToArray();

            var resourceViewModel = _compositionHost.GetExportedValue<ResourceViewModel>();

            resourceViewModel.Reload();

            var resourceManager = _compositionHost.GetExportedValue<ResourceManager>();

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

            var confirmed = ConfirmationDialog.Show(_compositionHost.Container, viewModel, Resources.MoveToResource).GetValueOrDefault();

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
        private static IEnumerable<ResourceEntity> GetResourceEntiesFromProject([NotNull] EnvDTE.Document document, [NotNull][ItemNotNull] IEnumerable<ResourceEntity> entities)
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

        private static bool IsInProject([NotNull] ResourceEntity entity, [CanBeNull] EnvDTE.Document document)
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

        private static bool IsInProject([NotNull] ResourceEntity entity, [CanBeNull] EnvDTE.Project project)
        {
            var projectFile = (DteProjectFile)entity.NeutralProjectFile;

            return projectFile?.ProjectItems
                .Select(item => item.ContainingProject)
                .Contains(project) ?? false;
        }

        [CanBeNull]
        private static Selection GetSelection([NotNull] EnvDTE.Document document)
        {
            var textDocument = (EnvDTE.TextDocument)document.Object(@"TextDocument");

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
            private readonly EnvDTE.TextDocument _textDocument;

            [CanBeNull]
            private readonly EnvDTE.FileCodeModel _codeModel;

            public Selection([NotNull] EnvDTE.TextDocument textDocument, [NotNull] string line, [CanBeNull] EnvDTE.FileCodeModel codeModel)
            {
                _textDocument = textDocument;
                Line = line;
                _codeModel = codeModel;
            }

            [NotNull]
            public EnvDTE.VirtualPoint Begin => _textDocument.Selection.TopPoint;

            [NotNull]
            public EnvDTE.VirtualPoint End => _textDocument.Selection.BottomPoint;

            public bool IsEmpty => Begin.EqualTo(End);

            [CanBeNull]
            public string Text => _textDocument.Selection?.Text;

            [NotNull]
            public string Line { get; }

            [CanBeNull]
            public string FunctionName => GetCodeElement(EnvDTE.vsCMElement.vsCMElementFunction)?.Name;

            [CanBeNull]
            public string ClassName => GetCodeElement(EnvDTE.vsCMElement.vsCMElementClass)?.Name;

            public void MoveTo(int startColumn, int endColumn)
            {
                var selection = _textDocument.Selection;
                if (selection == null)
                    return;

                selection.MoveToLineAndOffset(Begin.Line, startColumn);
                selection.MoveToLineAndOffset(Begin.Line, endColumn, true);
            }

            public void ReplaceWith([CanBeNull] string replacement)
            {
                var selection = _textDocument.Selection;
                // using "selection.Text = replacement" does not work here, since it will trigger auto-complete, 
                // and this may add unwanted additional characters, resulting in bad code.
                selection?.ReplaceText(selection.Text, replacement);
            }

            [CanBeNull]
            private EnvDTE.CodeElement GetCodeElement(EnvDTE.vsCMElement scope)
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
            [CanBeNull]
            string LocateString([CanBeNull] Selection selection, bool moveSelection);
        }

        private class GenericParser : IParser
        {
            [CanBeNull]
            public string LocateString([CanBeNull] Selection selection, bool moveSelection)
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
                    var firstQuote = line.IndexOf(@"""", secondQuote + 1, StringComparison.Ordinal);
                    if (firstQuote == -1)
                        return null;

                    if (line.Length <= firstQuote + 1)
                        return null;

                    if (column < firstQuote)
                        return null;

                    secondQuote = line.IndexOf(@"""", firstQuote + 1, StringComparison.Ordinal);
                    if (secondQuote == -1)
                        return null;

                    if (column >= secondQuote)
                        continue;

                    var startIndex = firstQuote + 1;
                    var length = secondQuote - firstQuote - 1;

                    if (moveSelection)
                    {
                        var startColumn = firstQuote + 1;
                        var endColumn = secondQuote + 2;

                        selection.MoveTo(startColumn, endColumn);
                    }

                    return line.Substring(startIndex, length);
                }

                return null;
            }
        }
    }
}
