namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using TomsToolbox.Essentials;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Export]
    internal class DteSolution
    {
        private const string SolutionItemsFolderName = "Solution Items";

        private readonly IServiceProvider _serviceProvider;
        private readonly ITracer _tracer;

        private IEnumerable<ProjectFile>? _projectFiles;

        [ImportingConstructor]
        public DteSolution([Import(nameof(VsPackage))][Ninject.Named(nameof(VsPackage))] IServiceProvider serviceProvider, ITracer tracer)
        {
            _serviceProvider = serviceProvider;
            _tracer = tracer;
        }

        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <param name="fileFilter"></param>
        /// <returns>The files.</returns>
        public IEnumerable<ProjectFile> GetProjectFiles(IFileFilter fileFilter)
        {
            ThrowIfNotOnUIThread();

            return _projectFiles ??= EnumerateProjectFiles(fileFilter) ?? new DirectoryInfo(SolutionFolder).GetAllSourceFiles(fileFilter, null);
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
                        _tracer.TraceWarning("Error loading project {0}[{1}]: {2}", name, index, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error loading projects: {0}", ex);
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
        private EnvDTE80.Solution2? Solution => Dte?.Solution as EnvDTE80.Solution2;

        private EnvDTE80.DTE2? Dte => _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        public EnvDTE.Globals? Globals
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

        public EnvDTE.ProjectItem? AddFile(string fullName)
        {
            ThrowIfNotOnUIThread();

            var solutionItemsProject = GetProjects().FirstOrDefault(IsSolutionItemsFolder) ?? Solution?.AddSolutionFolder(SolutionItemsFolderName);

            return solutionItemsProject?.AddFromFile(fullName);
        }

        public void Invalidate()
        {
            _projectFiles = null;
        }

        private static bool IsSolutionItemsFolder(EnvDTE.Project? project)
        {
            ThrowIfNotOnUIThread();

            if (project == null)
                return false;

            return string.Equals(project.Kind, ItemKind.SolutionFolder, StringComparison.OrdinalIgnoreCase)
                && string.Equals(project.Name, SolutionItemsFolderName, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<EnvDTE.Project> GetProjects()
        {
            ThrowIfNotOnUIThread();

            var solution = Solution;

            var projects = solution?.Projects;
            if (projects == null)
                yield break;

            for (var i = 1; i <= projects.Count; i++)
            {
                EnvDTE.Project project;
                try
                {
                    project = projects.Item(i);
                }
                catch
                {
                    _tracer.TraceError("Error loading project #" + i);
                    continue;
                }

                yield return project;
            }
        }

        private void GetProjectFiles(string? projectName, EnvDTE.ProjectItems? projectItems, IDictionary<string, DteProjectFile> items)
        {
            ThrowIfNotOnUIThread();

            if (projectItems == null)
                return;

            try
            {
                var index = 1;

                // Must use foreach here! See https://connect.microsoft.com/VisualStudio/feedback/details/1093318/resource-files-falsely-enumerated-as-part-of-project
                foreach (var projectItem in projectItems.OfType<EnvDTE.ProjectItem>())
                {
                    try
                    {
                        GetProjectFiles(projectName, projectItem, items);
                    }
                    catch
                    {
                        _tracer.TraceError("Error loading project item #{0} in project {1}.", index, projectName ?? "unknown");
                    }

                    index += 1;
                }
            }
            catch
            {
                _tracer.TraceError("Error loading a project item in project {0}.", projectName ?? "unknown");
            }
        }

        private void GetProjectFiles(string? projectName, EnvDTE.ProjectItem projectItem, IDictionary<string, DteProjectFile> items)
        {
            ThrowIfNotOnUIThread();

            if (projectItem.Object is VSLangProj.References) // MPF project (e.g. WiX) references folder, do not traverse...
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
                    }
                }
            }

            GetProjectFiles(projectName, projectItem.ProjectItems, items);

            if (projectItem.SubProject != null)
            {
                GetProjectFiles(projectName, projectItem.SubProject.ProjectItems, items);
            }
        }

        private string? TryGetFileName(EnvDTE.ProjectItem projectItem)
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
                _tracer.TraceWarning("Can't get filename for project item: {0} - {1}", name, projectItem.Kind);
            }

            return null;
        }
    }
}
