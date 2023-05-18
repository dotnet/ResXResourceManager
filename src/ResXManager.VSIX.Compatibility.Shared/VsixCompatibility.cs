namespace ResXManager.VSIX.Compatibility.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using Community.VisualStudio.Toolkit;

    using EnvDTE;

    using EnvDTE80;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using ResXManager.Model;

    using TomsToolbox.Essentials;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    using MessageBox = System.Windows.MessageBox;
    using Resources = Properties.Resources;

    [Export(typeof(IVsixCompatibility))]
    internal sealed class VsixCompatibility : IVsixCompatibility
    {
        private readonly ICustomToolRunner _customToolRunner;
        private readonly DteSolution _solution;

        public VsixCompatibility(ICustomToolRunner customToolRunner, DteSolution solution)
        {
            _customToolRunner = customToolRunner;
            _solution = solution;
        }

        public async Task<ICollection<string>> GetSelectedFilesAsync()
        {
            var monitorSelection = await VS.GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>().ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var selection = monitorSelection?.GetSelectedProjectItems();
            if (selection == null)
                return Array.Empty<string>();

            var files = selection
                .Select(item => item.GetMkDocument())
                .ExceptNullItems()
                .ToArray();

            return files;
        }

        public bool ContainsChildOfWinFormsDesignerItem(ResourceEntity entity, string? fileName)
        {
            ThrowIfNotOnUIThread();

            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName, StringComparison.OrdinalIgnoreCase) && projectFile.IsWinFormsDesignerResource);
        }

        public void RunCustomTool(IEnumerable<ResourceEntity> entities, string fileName)
        {
            // Run custom tool (usually attached to neutral language) even if a localized language changes,
            // e.g. if custom tool is a text template, we might want not only to generate the designer file but also
            // extract some localization information.
            // => find the resource entity that contains the document and run the custom tool on the neutral project file.

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
            bool Predicate(ResourceEntity e)
            {
                return e.Languages.Select(lang => lang.ProjectFile)
                    .OfType<DteProjectFile>()
                    .Any(projectFile => projectFile.ProjectItems.Any(p =>
                        string.Equals(p.Document?.FullName, fileName, StringComparison.OrdinalIgnoreCase)));
            }
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

            var entity = entities.FirstOrDefault(Predicate);

            var neutralProjectFile = entity?.NeutralProjectFile as DteProjectFile;

            // VS will run the custom tool on the project item only. Run the custom tool on any of the descendants, too.
            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.Descendants());

            _customToolRunner.Enqueue(projectItems);
        }

        public void RunCustomTool(ResourceEntity entity)
        {
            ThrowIfNotOnUIThread();

            var neutralProjectFile = entity.NeutralProjectFile as DteProjectFile;

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
            static IEnumerable<ProjectItem> GetDescendants(ProjectItem projectItem)
            {
                // VS will run the custom tool on the project item only if the document is open => Run the custom tool on any of the descendants, too.
                // VS will not run the custom tool if just the file is saved in background, and no document is open => Run the custom tool on all descendants and self.
                return projectItem.GetIsOpen() ? projectItem.Descendants() : projectItem.DescendantsAndSelf();
            }
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(GetDescendants);

            _customToolRunner.Enqueue(projectItems);
        }

        public async Task<bool> AffectsResourceFileAsync(string? fileName)
        {
            if (fileName == null)
                return false;

            var projectItem = await PhysicalFile.FromFileAsync(fileName).ConfigureAwait(false);
            if (projectItem == null)
                return false;

            return projectItem
                .DescendantsAndSelf()
                .OfType<PhysicalFile>()
                .Select(item => item.FullPath)
                .ExceptNullItems()
                .Select(Path.GetExtension)
                .Any(ProjectFileExtensions.IsSupportedFileExtension);
        }

        public void SetFontSize(DependencyObject view)
        {
            ThrowIfNotOnUIThread();

            var dte = ServiceProvider.GlobalProvider?.GetService(typeof(DTE)) as DTE2;

            dte?.SetFontSize(view);
        }

        public string EvaluateMoveToResourcePattern(string pattern, string? key, bool reuseExisting, ResourceEntity? selectedResourceEntity, ResourceTableEntry? selectedResourceEntry)
        {
            ThrowIfNotOnUIThread();

            var entity = reuseExisting ? selectedResourceEntry?.Container : selectedResourceEntity;
            var effectiveKey = reuseExisting ? selectedResourceEntry?.Key : key;
            var localNamespace = GetLocalNamespace((entity?.NeutralProjectFile as DteProjectFile)?.DefaultProjectItem);

            return pattern.Replace(@"$File", entity?.BaseName)
                .Replace(@"$Key", effectiveKey)
                .Replace(@"$Namespace", localNamespace);
        }

        private static string GetLocalNamespace(ProjectItem? resxItem)
        {
            ThrowIfNotOnUIThread();

            try
            {
                var resxPath = resxItem?.TryGetFileName();
                if (resxItem == null || resxPath == null)
                    return string.Empty;

                var resxFolder = Path.GetDirectoryName(resxPath);
                var project = resxItem.ContainingProject;
                var projectFolder = Path.GetDirectoryName(project?.FullName);
                var rootNamespace = project?.Properties?.Item(@"RootNamespace")?.Value?.ToString() ?? string.Empty;

                if ((resxFolder == null) || (projectFolder == null))
                    return string.Empty;

                var localNamespace = rootNamespace;
                if (resxFolder.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
                {
                    localNamespace += resxFolder.Substring(projectFolder.Length)
                        .Replace('\\', '.')
                        .Replace("My Project", "My"); // VB workaround
                }

                return localNamespace;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public bool ActivateAlreadyOpenEditor(IEnumerable<ResourceLanguage> languages)
        {
            ThrowIfNotOnUIThread();

            var alreadyOpenItems = GetLanguagesOpenedInAnotherEditor(languages);

            string message;

            if (alreadyOpenItems.Any())
            {
                message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorOpenFilesInEditor, FormatFileNames(alreadyOpenItems.Select(item => item.Item1)));
                MessageBox.Show(message, Model.Properties.Resources.Title);

                ActivateWindow(alreadyOpenItems.Select(item => item.Item2).FirstOrDefault());

                return true;
            }

            return false;
        }

        [Localizable(false)]
        private static string FormatFileNames(IEnumerable<string> fileNames)
        {
            return string.Join("\n", fileNames.Select(x => "\xA0-\xA0" + x));
        }

        private static void ActivateWindow(EnvDTE.Window? window)
        {
            try
            {
                ThrowIfNotOnUIThread();

                window?.Activate();
            }
            catch
            {
                // Something is wrong with the window, we can't do anything about this...
            }
        }

        private static Tuple<string, EnvDTE.Window>[] GetLanguagesOpenedInAnotherEditor(IEnumerable<ResourceLanguage> languages)
        {
            ThrowIfNotOnUIThread();

            if (ServiceProvider.GlobalProvider?.GetService(typeof(DTE)) is not DTE2 dte)
                return Array.Empty<Tuple<string, EnvDTE.Window>>();

            try
            {
                ThrowIfNotOnUIThread();

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
                var openDocuments = dte.Windows?
                    .OfType<EnvDTE.Window>()
                    .Where(window => window.Visible && (window.Document != null))
                    .ToDictionary(window => window.Document);

                var items = from l in languages
                            let file = l.FileName
                            let projectFile = l.ProjectFile as DteProjectFile
                            let documents = projectFile?.ProjectItems.Select(item => item.TryGetDocument()).Where(doc => doc != null)
                            let window = documents?.Select(doc => openDocuments?.GetValueOrDefault(doc)).FirstOrDefault(win => win != null)
                            where window != null
                            select Tuple.Create(file, window);
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

                return items.ToArray();
            }
            catch
            {
                return Array.Empty<Tuple<string, EnvDTE.Window>>();
            }
        }

        public void AddProjectItems(ResourceEntity entity, ResourceLanguage neutralLanguage, string languageFileName)
        {
            ThrowIfNotOnUIThread();

            DteProjectFile? projectFile = null;

            var projectItems = (neutralLanguage.ProjectFile as DteProjectFile)?.ProjectItems;
            if (projectItems == null)
            {
                entity.AddLanguage(new ProjectFile(languageFileName, entity.Container.SolutionFolder ?? string.Empty, entity.ProjectName, null));
                return;
            }

            foreach (var neutralLanguageProjectItem in projectItems)
            {
                var collection = neutralLanguageProjectItem.Collection;
                var projectItem = collection.AddFromFile(languageFileName);
                var containingProject = projectItem.ContainingProject;
                var projectName = containingProject.Name;
                if (projectFile == null)
                {
                    var solution = _solution;

                    projectFile = new DteProjectFile(solution, solution.SolutionFolder, languageFileName, projectName, containingProject.UniqueName, projectItem);
                }
                else
                {
                    projectFile.AddProject(projectName, projectItem);
                }
            }

            if (projectFile != null)
            {
                entity.AddLanguage(projectFile);
            }
        }


    }
}
