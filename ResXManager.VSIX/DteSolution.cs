﻿namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    [Export]
    internal class DteSolution
    {
        private const string SolutionItemsFolderName = "Solution Items";

        [NotNull]
        private readonly IServiceProvider _serviceProvider;
        [NotNull]
        private readonly ITracer _tracer;

        [CanBeNull]
        [ItemNotNull]
        private IEnumerable<DteProjectFile> _projectFiles;

        [ImportingConstructor]
        public DteSolution([NotNull][Import(nameof(VSPackage))] IServiceProvider serviceProvider, [NotNull] ITracer tracer)
        {
            Contract.Requires(serviceProvider != null);
            Contract.Requires(tracer != null);

            _serviceProvider = serviceProvider;
            _tracer = tracer;
        }

        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <returns>The files.</returns>
        [NotNull, ItemNotNull]
        public IEnumerable<DteProjectFile> GetProjectFiles()
        {
            Contract.Ensures(Contract.Result<IEnumerable<ProjectFile>>() != null);

            return _projectFiles ?? (_projectFiles = EnumerateProjectFiles());
        }

        [NotNull, ItemNotNull]
        private IEnumerable<DteProjectFile> EnumerateProjectFiles()
        {
            Contract.Ensures(Contract.Result<IEnumerable<DteProjectFile>>() != null);

            var items = new Dictionary<string, DteProjectFile>();

            try
            {
                var index = 0;

                foreach (var project in GetProjects())
                {
                    Contract.Assume(project != null);

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

            return items.Values;
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        [CanBeNull, ItemNotNull]
        private EnvDTE80.Solution2 Solution => Dte?.Solution as EnvDTE80.Solution2;

        [CanBeNull]
        private EnvDTE80.DTE2 Dte => _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        [CanBeNull]
        public EnvDTE.Globals Globals
        {
            get
            {
                var solution = Solution;

                return string.IsNullOrEmpty(FullName) ? null : solution?.Globals;
            }
        }

        [NotNull]
        public string SolutionFolder
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                try
                {
                    return Path.GetDirectoryName(FullName) ?? string.Empty;
                }
                catch
                {
                }

                return string.Empty;
            }
        }

        [CanBeNull]
        public string FullName => Solution?.FullName;

        [CanBeNull]
        public EnvDTE.ProjectItem AddFile([NotNull] string fullName)
        {
            Contract.Requires(fullName != null);

            var solutionItemsProject = GetProjects().FirstOrDefault(IsSolutionItemsFolder) ?? Solution?.AddSolutionFolder(SolutionItemsFolderName);

            return solutionItemsProject?.AddFromFile(fullName);
        }

        public void Invalidate()
        {
            _projectFiles = null;
        }

        private static bool IsSolutionItemsFolder([CanBeNull] EnvDTE.Project project)
        {
            if (project == null)
                return false;

            return string.Equals(project.Kind, ItemKind.SolutionFolder, StringComparison.OrdinalIgnoreCase)
                && string.Equals(project.Name, SolutionItemsFolderName, StringComparison.CurrentCultureIgnoreCase);
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<EnvDTE.Project> GetProjects()
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.Project>>() != null);

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

        private void GetProjectFiles([CanBeNull] string projectName, [ItemNotNull][CanBeNull] EnvDTE.ProjectItems projectItems, [NotNull] IDictionary<string, DteProjectFile> items)
        {
            Contract.Requires(items != null);

            if (projectItems == null)
                return;

            try
            {
                var index = 1;

                // Must use forach here! See https://connect.microsoft.com/VisualStudio/feedback/details/1093318/resource-files-falsely-enumerated-as-part-of-project
                foreach (var projectItem in projectItems.OfType<EnvDTE.ProjectItem>())
                {
                    try
                    {
                        GetProjectFiles(projectName, projectItem, items);
                    }
                    catch
                    {
                        _tracer.TraceError("Error loading project item #{0} in project {1}.", index, projectName);
                    }

                    index += 1;
                }
            }
            catch
            {
                _tracer.TraceError("Error loading a project item in project {0}.", projectName);
            }
        }

        private void GetProjectFiles([CanBeNull] string projectName, [NotNull] EnvDTE.ProjectItem projectItem, [NotNull] IDictionary<string, DteProjectFile> items)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(items != null);

            if (projectItem.Object is VSLangProj.References) // MPF project (e.g. WiX) references folder, do not traverse...
                return;

            if (projectItem.FileCount > 0)
            {
                var fileName = TryGetFileName(projectItem);

                if (!string.IsNullOrEmpty(fileName))
                {
                    var project = projectItem.ContainingProject;
                    Contract.Assume(project != null);

                    FileInfo projectFileInfo = new FileInfo(project.FullName);
                    var assemblyName = ResourceManagerExtensions.GetValueOrDefaultFromCsProj(projectFileInfo, "AssemblyName", project.Name);
                    var rootNamespace = ResourceManagerExtensions.GetValueOrDefaultFromCsProj(projectFileInfo, "RootNamespace", project.Name);

                    if (items.TryGetValue(fileName, out var projectFile))
                    {
                        Contract.Assume(projectFile != null);
                        Contract.Assume(project.Name != null);

                        projectFile.AssemblyName = assemblyName;
                        projectFile.RootNamespace = rootNamespace;
                        projectFile.AddProject(project.Name, projectItem);
                    }
                    else
                    {
                        items.Add(fileName, new DteProjectFile(this, fileName, project.Name, project.UniqueName, projectItem)
                        {
                            AssemblyName = assemblyName,
                            RootNamespace = rootNamespace
                        });
                    }
                }
            }

            GetProjectFiles(projectName, projectItem.ProjectItems, items);

            if (projectItem.SubProject != null)
            {
                GetProjectFiles(projectName, projectItem.SubProject.ProjectItems, items);
            }
        }

        [CanBeNull]
        private string TryGetFileName([NotNull] EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            var name = projectItem.Name;
            Contract.Assume(name != null);

            try
            {
                if (string.Equals(projectItem.Kind, ItemKind.PhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    // some items report a file count > 0 but don't return a file name!
                    return projectItem.FileNames[0];
                }

                if (string.Equals(projectItem.Kind, ItemKind.SolutionFile, StringComparison.OrdinalIgnoreCase))
                {
                    var solutionFolder = SolutionFolder;
                    if (!string.IsNullOrEmpty(solutionFolder))
                        return Path.Combine(solutionFolder, name);
                }
            }
            catch (ArgumentException)
            {
                _tracer.TraceWarning("Can't get filename for project item: {0} - {1}", name, projectItem.Kind);
            }

            return null;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_serviceProvider != null);
            Contract.Invariant(_tracer != null);
        }
    }
}
