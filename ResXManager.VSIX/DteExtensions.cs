namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;

    internal static class DteExtensions
    {
        /// <summary>
        /// Gets all files of all project in the solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The file names of all files.</returns>
        public static IEnumerable<DteProjectFile> GetProjectFiles(this EnvDTE.Solution solution, ITracer trace)
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

        /// <summary>
        /// Gets the projects of the solution, ignoring the ones that fail to load.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="trace">The tracer.</param>
        /// <returns>The projects.</returns>
        private static IEnumerable<EnvDTE.Project> GetProjects(this EnvDTE._Solution solution, ITracer trace)
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

        private static void GetProjectFiles(this EnvDTE.ProjectItems projectItems, string solutionFolder, IDictionary<string, DteProjectFile> items, ITracer trace)
        {
            Contract.Requires(solutionFolder != null);
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
                        projectItem.GetProjectFiles(solutionFolder, items, trace);
                    }
                    catch
                    {
                        trace.TraceError("Error loading project item #{0}.", index);
                    }

                    index += 1;
                }
            }
            catch
            {
                trace.TraceError("Error loading a project item.");
            }
        }

        private static void GetProjectFiles(this EnvDTE.ProjectItem projectItem, string solutionFolder, IDictionary<string, DteProjectFile> items, ITracer trace)
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

        public static IEnumerable<VSITEMSELECTION> GetSelectedProjectItems(this IVsMonitorSelection monitorSelection)
        {
            Contract.Requires(monitorSelection != null);
            Contract.Ensures(Contract.Result<IEnumerable<VSITEMSELECTION>>() != null);

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                IVsMultiItemSelect multiItemSelect;
                uint itemId;

                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemId, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr))
                    return Enumerable.Empty<VSITEMSELECTION>();

                if ((itemId == VSConstants.VSITEMID_SELECTION) && (multiItemSelect != null))
                {
                    uint cItems;
                    int info;

                    multiItemSelect.GetSelectionInfo(out cItems, out info);
                    var items = new VSITEMSELECTION[cItems];
                    multiItemSelect.GetSelectedItems(0, cItems, items);
                    return items;
                }

                if ((hierarchyPtr == IntPtr.Zero) || (itemId == VSConstants.VSITEMID_ROOT))
                    return Enumerable.Empty<VSITEMSELECTION>();

                return new[]
                {
                    new VSITEMSELECTION
                    {
                        pHier = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy, 
                        itemid = itemId
                    }
                };
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                    Marshal.Release(selectionContainerPtr);

                if (hierarchyPtr != IntPtr.Zero)
                    Marshal.Release(hierarchyPtr);
            }
        }

        public static string GetMkDocument(this VSITEMSELECTION selection)
        {
            string itemFullPath;

            var vsProject = selection.pHier as IVsProject;

            if (vsProject == null)
                return null;

            vsProject.GetMkDocument(selection.itemid, out itemFullPath);

            return itemFullPath;
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
                var textDocument = (EnvDTE.TextDocument)document.Object("TextDocument");
                textDocument.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);
                document.Save();
                return true;
            }
            catch
            {
            }

            return false;
        }

        public static IEnumerable<EnvDTE.ProjectItem> DescendantsAndSelf(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (projectItem == null)
                yield break;

            yield return projectItem;

            var projectItems = projectItem.ProjectItems;

            if (projectItems == null)
                yield break;

            foreach (var item in projectItems.OfType<EnvDTE.ProjectItem>().SelectMany(p => p.DescendantsAndSelf()))
            {
                yield return item;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void SetFontSize(this EnvDTE.DTE dte, DependencyObject view)
        {
            Contract.Requires(dte != null);
            Contract.Requires(view != null);

            const string CATEGORY_FONTS_AND_COLORS = "FontsAndColors";
            const string PAGE_TEXT_EDITOR = "TextEditor";
            const string PROPERTY_FONT_SIZE = "FontSize";

            try
            {
                var fontSize = dte.Maybe()
                    .Select(x => x.Properties[CATEGORY_FONTS_AND_COLORS, PAGE_TEXT_EDITOR])
                    .Select(x => x.Item(PROPERTY_FONT_SIZE))
                    .Select(x => x.Value)
                    .Return(x => Convert.ToDouble(x, CultureInfo.InvariantCulture));

                if (fontSize > 1)
                {
                    // Default in VS is 10, but looks like 12 in WPF
                    view.SetValue(Appearance.TextFontSizeProperty, fontSize * 1.2);
                }
            }
            catch
            {
            }
        }


    }
}
