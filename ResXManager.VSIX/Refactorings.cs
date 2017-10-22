namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;
    using tomenglertde.ResXManager.VSIX.Visuals;

    using TomsToolbox.Desktop.Composition;

    public interface IRefactorings
    {
        bool CanMoveToResource(EnvDTE.Document document);

        ResourceTableEntry MoveToResource(EnvDTE.Document document);
    }

    [Export(typeof(IRefactorings))]
    internal class Refactorings : IRefactorings
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost;
        private ResourceEntity _lastUsedEntity;

        [ImportingConstructor]
        public Refactorings([NotNull] ICompositionHost compositionHost)
        {
            Contract.Requires(compositionHost != null);

            _compositionHost = compositionHost;
        }

        public bool CanMoveToResource(EnvDTE.Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return false;

            var configuration = _compositionHost.GetExportedValue<DteConfiguration>();

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

            var s = parser.LocateString(selection, false);

            return s != null;
        }

        public ResourceTableEntry MoveToResource(EnvDTE.Document document)
        {
            var extension = Path.GetExtension(document?.FullName);
            if (extension == null)
                return null;

            var configurationItems = _compositionHost.GetExportedValue<DteConfiguration>().MoveToResources.Items;

            var configuration = configurationItems
                .FirstOrDefault(item => item.ParseExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (configuration == null)
                return null;

            Contract.Assume(document != null);
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
                .ToArray();

            // ReSharper disable once PossibleNullReferenceException
            var filter = Settings.Default.ResourceFilter?.Trim();

            if (!string.IsNullOrEmpty(filter))
            {
                var regex = new Regex(filter, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                entities = entities
                    .Where(item => regex.Match(item.ToString()).Success)
                    .ToArray();
            }

            var favorites = new[] { GetPreferredResourceEntity(document, entities), _lastUsedEntity }
                .Where(item => item != null)
                .ToArray();

            entities = favorites
                .Concat(entities.Except(favorites))
                .ToArray();

            var viewModel = new MoveToResourceViewModel(patterns, entities, text, extension, selection.ClassName, selection.FunctionName);

            var confirmed = ConfirmationDialog.Show(_compositionHost.Container, viewModel, Resources.MoveToResource).GetValueOrDefault();

            if (confirmed && !string.IsNullOrEmpty(viewModel.Key))
            {
                ResourceTableEntry entry = null;

                if (!viewModel.ReuseExisiting)
                {
                    var entity = _lastUsedEntity = viewModel.SelectedResourceEntity;

                    entry = entity?.Add(viewModel.Key);
                    if (entry == null)
                        return null;

                    entry.Values[null] = viewModel.Value;
                    entry.Comment = viewModel.Comment;
                }

                selection.ReplaceWith(viewModel.Replacement?.Value);

                return entry;
            }

            return null;
        }

        private static ResourceEntity GetPreferredResourceEntity([NotNull] EnvDTE.Document document, [NotNull][ItemNotNull] IEnumerable<ResourceEntity> entities)
        {
            Contract.Requires(document != null);
            Contract.Requires(entities != null);

            try
            {
                var project = document.ProjectItem?.ContainingProject;

                return entities.FirstOrDefault(entity => IsInProject(entity, project));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsInProject([NotNull] ResourceEntity entity, EnvDTE.Project project)
        {
            Contract.Requires(entity != null);

            var projectFile = (DteProjectFile)entity.NeutralProjectFile;

            return projectFile?.ProjectItems
                .Select(item => item.ContainingProject)
                .Contains(project) ?? false;
        }

        private static Selection GetSelection([NotNull] EnvDTE.Document document)
        {
            Contract.Requires(document != null);

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
            [NotNull]
            private readonly string _line;
            private readonly EnvDTE.FileCodeModel _codeModel;

            public Selection([NotNull] EnvDTE.TextDocument textDocument, [NotNull] string line, EnvDTE.FileCodeModel codeModel)
            {
                Contract.Requires(textDocument != null);
                Contract.Requires(line != null);

                _textDocument = textDocument;
                _line = line;
                _codeModel = codeModel;
            }

            [ContractVerification(false)]
            [NotNull]
            public EnvDTE.VirtualPoint Begin
            {
                get
                {
                    Contract.Ensures(Contract.Result<EnvDTE.VirtualPoint>() != null);
                    return _textDocument.Selection.TopPoint;
                }
            }

            [ContractVerification(false)]
            [NotNull]
            public EnvDTE.VirtualPoint End
            {
                get
                {
                    Contract.Ensures(Contract.Result<EnvDTE.VirtualPoint>() != null);
                    return _textDocument.Selection.BottomPoint;
                }
            }

            public bool IsEmpty => Begin.EqualTo(End);

            public string Text => _textDocument.Selection?.Text;

            [NotNull]
            public string Line
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);
                    return _line;
                }
            }

            public string FunctionName => GetCodeElement(EnvDTE.vsCMElement.vsCMElementFunction)?.Name;

            public string ClassName => GetCodeElement(EnvDTE.vsCMElement.vsCMElementClass)?.Name;

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
                // using "selection.Text = replacement" does not work here, since it will trigger auto-complete, 
                // and this may add unwanted additional characters, resulting in bad code.
                selection?.ReplaceText(selection.Text, replacement);
            }

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

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_textDocument != null);
                Contract.Invariant(_line != null);
            }
        }

        private interface IParser
        {
            string LocateString(Selection selection, bool moveSelection);
        }

        private class GenericParser : IParser
        {
            public string LocateString(Selection selection, bool moveSelection)
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
                    Contract.Assume(startIndex + length <= line.Length);

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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
        }
    }
}
