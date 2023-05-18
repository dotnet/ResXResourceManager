namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Windows.Threading;

    using EnvDTE;

    using EnvDTE80;

    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.VSIX.Compatibility;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    using VSLangProj;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Export]
    internal sealed class DteSolution
    {
        private const string SolutionItemsFolderName = "Solution Items";

        private readonly DTE2 _dte;

        private IEnumerable<ProjectFile>? _projectFiles;

        [ImportingConstructor]
        public DteSolution(ITracer tracer)
        {
            ThrowIfNotOnUIThread();

            Tracer = tracer;
            _dte = (DTE2)(ServiceProvider.GlobalProvider.GetService(typeof(DTE)) ?? throw new InvalidOperationException("Can't get DTE service"));
        }

        public ITracer Tracer { get; }

        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <param name="fileFilter"></param>
        /// <returns>The files.</returns>
        public IEnumerable<ProjectFile> GetProjectFiles(IFileFilter fileFilter)
        {
            ThrowIfNotOnUIThread();

            return _projectFiles = EnumerateProjectFiles(fileFilter) ?? new DirectoryInfo(SolutionFolder).GetAllSourceFiles(fileFilter, null);
        }

        public IEnumerable<ProjectFile>? GetCachedProjectFiles()
        {
            return _projectFiles;
        }

        private IEnumerable<ProjectFile>? EnumerateProjectFiles(IFileFilter fileFilter)
        {
            ThrowIfNotOnUIThread();

            return EnumerateProjectFiles()?.Where(fileFilter.Matches);
        }

        private IEnumerable<ProjectFile>? EnumerateProjectFiles()
        {
            ThrowIfNotOnUIThread();

            var items = new Dictionary<string, DteProjectFile>();

            try
            {
                var index = 0;

                foreach (var project in GetProjects())
                {
                    var name = @"<unknown>";

                    try
                    {
                        index += 1;
                        name = project.Name;

                        GetProjectFiles(name, project.ProjectItems, items);
                    }
                    catch (Exception ex)
                    {
                        Tracer.TraceWarning("Error loading project {0}[{1}]: {2}", name, index, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("Error loading projects: {0}", ex);
            }

            var files = items.Values;
            if (!files.Any())
            {
                var solutionFolder = SolutionFolder;
                var fullName = FullName;

                if (!solutionFolder.IsNullOrEmpty() && string.Equals(solutionFolder, fullName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            return files;
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        private Solution2? Solution => _dte.Solution as Solution2;

        public Globals? Globals
        {
            get
            {
                ThrowIfNotOnUIThread();

                var solution = Solution;

                return FullName.IsNullOrEmpty() ? null : solution?.Globals;
            }
        }

        public string SolutionFolder
        {
            get
            {
                ThrowIfNotOnUIThread();

                try
                {
                    var fullName = FullName;

                    if (fullName.IsNullOrEmpty())
                        return string.Empty;
                    if (new DirectoryInfo(fullName).Exists)
                        return fullName;

                    return Path.GetDirectoryName(fullName) ?? string.Empty;
                }
                catch
                {
                    // just go with empty folder...
                }

                return string.Empty;
            }
        }

        public string? FullName
        {
            get
            {
                ThrowIfNotOnUIThread();

                return Solution?.FullName;
            }
        }

        public ProjectItem? AddFile(string fullName)
        {
            ThrowIfNotOnUIThread();

            var solutionItemsProject = GetProjects().FirstOrDefault(IsSolutionItemsFolder) ?? Solution?.AddSolutionFolder(SolutionItemsFolderName);

            return solutionItemsProject?.AddFromFile(fullName);
        }

        private static bool IsSolutionItemsFolder(Project? project)
        {
            ThrowIfNotOnUIThread();

            if (project == null)
                return false;

            return string.Equals(project.Kind, ItemKind.SolutionFolder, StringComparison.OrdinalIgnoreCase)
                && string.Equals(project.Name, SolutionItemsFolderName, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<Project> GetProjects()
        {
            ThrowIfNotOnUIThread();

            var solution = Solution;

            var projects = solution?.Projects;
            if (projects == null)
                yield break;

            for (var i = 1; i <= projects.Count; i++)
            {
                Project project;
                try
                {
                    project = projects.Item(i);
                }
                catch
                {
                    Tracer.TraceError("Error loading project #" + i);
                    continue;
                }

                yield return project;
            }
        }

        private void GetProjectFiles(string? projectName, ProjectItems? projectItems, IDictionary<string, DteProjectFile> items)
        {
            ThrowIfNotOnUIThread();

            if (projectItems == null)
                return;

            try
            {
                var index = 1;

                // Must use foreach here! See https://connect.microsoft.com/VisualStudio/feedback/details/1093318/resource-files-falsely-enumerated-as-part-of-project
                foreach (var projectItem in projectItems.OfType<ProjectItem>())
                {
                    try
                    {
                        GetProjectFiles(projectName, projectItem, items);
                    }
                    catch
                    {
                        Tracer.TraceError("Error loading project item #{0} in project {1}.", index, projectName ?? "unknown");
                    }

                    index += 1;
                }
            }
            catch
            {
                Tracer.TraceError("Error loading a project item in project {0}.", projectName ?? "unknown");
            }
        }

        private void GetProjectFiles(string? projectName, ProjectItem projectItem, IDictionary<string, DteProjectFile> items)
        {
            ThrowIfNotOnUIThread();

            if (projectItem.Object is References) // MPF project (e.g. WiX) references folder, do not traverse...
                return;

            if (projectItem.FileCount > 0)
            {
                var fileName = TryGetFileName(projectItem);

                if (!fileName.IsNullOrEmpty())
                {
                    var project = projectItem.ContainingProject;

                    if (items.TryGetValue(fileName, out var projectFile))
                    {
                        projectFile.AddProject(project.Name, projectItem);
                    }
                    else
                    {
                        items.Add(fileName, new DteProjectFile(this, SolutionFolder, fileName, project.Name, project.UniqueName, projectItem));

                        if ((items.Count % 256) == 0)
                        {
                            Dispatcher.CurrentDispatcher.ProcessMessages();
                        }
                    }
                }
            }

            GetProjectFiles(projectName, projectItem.ProjectItems, items);

            if (projectItem.SubProject != null)
            {
                GetProjectFiles(projectName, projectItem.SubProject.ProjectItems, items);
            }
        }

        private string? TryGetFileName(ProjectItem projectItem)
        {
            ThrowIfNotOnUIThread();

            var name = projectItem.Name;

            try
            {
                if (string.Equals(projectItem.Kind, ItemKind.PhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    // some items report a file count > 0 but don't return a file name!
                    return projectItem.FileNames[0];
                }

                if (string.Equals(projectItem.Kind, ItemKind.SolutionFile, StringComparison.OrdinalIgnoreCase))
                {
                    // solution file seems to be one-based
                    return projectItem.FileNames[1];
                }
            }
            catch (ArgumentException)
            {
                Tracer.TraceWarning("Can't get filename for project item: {0} - {1}", name, projectItem.Kind);
            }

            return null;
        }
    }
}
