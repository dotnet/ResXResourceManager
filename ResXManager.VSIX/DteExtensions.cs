namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using EnvDTE;

    using tomenglertde.ResXManager.Model;

    internal static class DteExtensions
    {
        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The file names of all files.</returns>
        public static IEnumerable<DteProjectFile> GetProjectFiles(this Solution solution, OutputWindowTracer trace)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solution.Projects != null);
            Contract.Requires(trace != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectFile>>() != null);

            var items = new Dictionary<string, DteProjectFile>();

            foreach (var project in solution.GetProjects(trace))
            {
                trace.WriteLine("Loading project " + project.Name);

                project.ProjectItems.GetProjectFiles(items, trace);
            }

            return items.Values;
        }

        public static string GetItemType(this DteProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var properties = projectFile.ProjectItems.First().Properties;

            if (properties == null)
                return string.Empty;

            return properties.OfType<Property>()
                .Where(p => p.Name == "ItemType")
                .Select(p => p.Value as string)
                .FirstOrDefault() ?? string.Empty;
        }


        public static bool IsSourceCodeFile(this DteProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var itemType = projectFile.GetItemType();

            return (itemType == "Compile") || (itemType == "Page");
        }

        /// <summary>
        /// Gets the projects of the solution, ignoring the ones that fail to load.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The projects.</returns>
        private static IEnumerable<EnvDTE.Project> GetProjects(this _Solution solution, OutputWindowTracer trace)
        {
            Contract.Requires(solution != null);
            Contract.Requires(solution.Projects != null);
            Contract.Requires(trace != null);

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

        private static void GetProjectFiles(this ProjectItems projectItems, IDictionary<string, DteProjectFile> items, OutputWindowTracer trace)
        {
            Contract.Requires(items != null);

            if (projectItems == null)
                return;

            for (var i = 1; i <= projectItems.Count; i++)
            {
                try
                {
                    var projectItem = projectItems.Item(i);
                    projectItem.GetProjectFiles(items, trace);
                }
                catch
                {
                    trace.TraceError("Error loading project item #" + i);
                }
            }
        }

        private static void GetProjectFiles(this ProjectItem projectItem, IDictionary<string, DteProjectFile> items, OutputWindowTracer trace)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(items != null);

            if (projectItem.FileCount > 0)
            {
                var fileName = TryGetFileName(projectItem);

                if (fileName != null)
                {
                    var project = projectItem.ContainingProject;
                    Contract.Assume(project != null);

                    DteProjectFile projectFile;
                    if (items.TryGetValue(fileName, out projectFile))
                    {
                        Contract.Assume(projectFile != null);
                        projectFile.AddProject(project.Name, projectItem);
                    }
                    else
                    {
                        items.Add(fileName, new DteProjectFile(fileName, project.Name, project.UniqueName, projectItem));
                    }
                }
            }

            projectItem.ProjectItems.GetProjectFiles(items, trace);

            if (projectItem.SubProject != null) 
            {
                projectItem.SubProject.ProjectItems.GetProjectFiles(items, trace);
            }
        }

        private static string TryGetFileName(this ProjectItem projectItem)
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
    }
}
