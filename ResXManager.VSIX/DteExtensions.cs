namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using tomenglertde.ResXManager.Model;

    internal static class DteExtensions
    {
        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The file names of all files.</returns>
        public static IEnumerable<DteProjectFile> GetProjectFiles(this EnvDTE.Solution solution, OutputWindowTracer trace)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solution.Projects != null);
            Contract.Requires(trace != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectFile>>() != null);

            var items = new Dictionary<string, DteProjectFile>();

            var solutionFolder = solution.GetSolutionFolder();

            foreach (var project in solution.GetProjects(trace))
            {
                Contract.Assume(project != null);

                trace.WriteLine("Loading project " + project.Name);

                project.ProjectItems.GetProjectFiles(solutionFolder, items, trace);
            }

            return items.Values;
        }

        private static string GetSolutionFolder(this EnvDTE._Solution solution)
        {
            Contract.Requires(solution != null);
            Contract.Ensures(Contract.Result<string>() != null);
            Contract.Ensures(solution.Projects == Contract.OldValue(solution.Projects));

            try
            {
                var fullName = solution.FullName;

                if (!string.IsNullOrEmpty(fullName))
                {
                    return Path.GetDirectoryName(fullName);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        public static string GetItemType(this DteProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var properties = projectFile.ProjectItems.First().Properties;

            if (properties == null)
                return string.Empty;

            return properties.OfType<EnvDTE.Property>()
                .Where(p => p.Name == "ItemType")
                .Select(p => p.Value as string)
                .FirstOrDefault() ?? string.Empty;
        }

        public static bool IsSourceCodeOrContentFile(this DteProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var itemType = projectFile.GetItemType();

            return (itemType == "Compile") || (itemType == "Page") || (itemType == "Content");
        }

        /// <summary>
        /// Gets the projects of the solution, ignoring the ones that fail to load.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The projects.</returns>
        private static IEnumerable<EnvDTE.Project> GetProjects(this EnvDTE._Solution solution, OutputWindowTracer trace)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solution.Projects != null);
            Contract.Requires(trace != null);
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.Project>>() != null);

            var projects = solution.Projects;

            for (var i = 1; i <= projects.Count; i++)
            {
                EnvDTE.Project project;
                try
                {
                    project = projects.Item(i);
                }
                catch
                {
                    trace.TraceError("Error loading project #" + i);
                    continue;
                }

                yield return project;
            }
        }

        private static void GetProjectFiles(this EnvDTE.ProjectItems projectItems, string solutionFolder, IDictionary<string, DteProjectFile> items, OutputWindowTracer trace)
        {
            Contract.Requires(solutionFolder != null);
            Contract.Requires(items != null);

            if (projectItems == null)
                return;

            for (var i = 1; i <= projectItems.Count; i++)
            {
                try
                {
                    var projectItem = projectItems.Item(i);
                    Contract.Assume(projectItem != null);
                    projectItem.GetProjectFiles(solutionFolder, items, trace);
                }
                catch
                {
                    trace.TraceError("Error loading project item #" + i);
                }
            }
        }

        private static void GetProjectFiles(this EnvDTE.ProjectItem projectItem, string solutionFolder, IDictionary<string, DteProjectFile> items, OutputWindowTracer trace)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(solutionFolder != null);
            Contract.Requires(items != null);


            if (projectItem.FileCount > 0)
            {
                var fileName = TryGetFileName(projectItem);

                if (!string.IsNullOrEmpty(fileName))
                {
                    var project = projectItem.ContainingProject;
                    Contract.Assume(project != null);

                    DteProjectFile projectFile;
                    if (items.TryGetValue(fileName, out projectFile))
                    {
                        Contract.Assume(projectFile != null);
                        Contract.Assume(project.Name != null);

                        projectFile.AddProject(project.Name, projectItem);
                    }
                    else
                    {
                        items.Add(fileName, new DteProjectFile(fileName, solutionFolder, project.Name, project.UniqueName, projectItem));
                    }
                }
            }

            projectItem.ProjectItems.GetProjectFiles(solutionFolder, items, trace);

            if (projectItem.SubProject != null)
            {
                projectItem.SubProject.ProjectItems.GetProjectFiles(solutionFolder, items, trace);
            }
        }

        private static string TryGetFileName(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            try
            {
                // some items report a file count > 0 but don't return a file name!
                return projectItem.FileNames[0];
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        public static string TryGetContent(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            if (!projectItem.IsOpen)
                return null;

            var document = projectItem.Document;
            if (document == null)
                return null;

            return TryGetContent(document);
        }

        [ContractVerification(false)]
        private static string TryGetContent(EnvDTE.Document document)
        {
            try
            {
                var textDocument = (EnvDTE.TextDocument)document.Object("TextDocument");
                return textDocument.CreateEditPoint().GetText(textDocument.EndPoint);
            }
            catch
            {
            }

            return null;
        }

        public static bool TrySetContent(this EnvDTE.ProjectItem projectItem, string text)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(text != null);

            if (!projectItem.IsOpen)
                return false;

            var document = projectItem.Document;
            if (document == null)
                return false;

            return TrySetContent(text, document);
        }

        [ContractVerification(false)]
        private static bool TrySetContent(string text, EnvDTE.Document document)
        {
            try
            {
                var textDocument = (EnvDTE.TextDocument) document.Object("TextDocument");
                textDocument.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);
                document.Save();
                return true;
            }
            catch
            {
            }

            return false;
        }
    }
}
