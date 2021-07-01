namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;

    using Microsoft.VisualStudio;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    using ResXManager.View;

    internal static class DteExtensions
    {
        public static EnvDTE.Document? TryGetDocument(this EnvDTE.ProjectItem? projectItem)
        {
            ThrowIfNotOnUIThread();

            try
            {
                if (projectItem?.IsOpen != true)
                    return null;

                projectItem.Open();
                return projectItem.Document;
            }
            catch
            {
                return null;
            }
        }

        public static bool GetIsOpen(this EnvDTE.ProjectItem projectItem)
        {
            ThrowIfNotOnUIThread();

            try
            {
                return projectItem.IsOpen;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static ICollection<VSITEMSELECTION> GetSelectedProjectItems(this IVsMonitorSelection monitorSelection)
        {
            ThrowIfNotOnUIThread();

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out var itemId, out var multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr))
                    return Array.Empty<VSITEMSELECTION>();

                if ((itemId == VSConstants.VSITEMID_SELECTION) && (multiItemSelect != null))
                {
                    multiItemSelect.GetSelectionInfo(out var cItems, out _);
                    var items = new VSITEMSELECTION[cItems];
                    multiItemSelect.GetSelectedItems(0, cItems, items);
                    return items;
                }

                if ((hierarchyPtr == IntPtr.Zero) || (itemId == VSConstants.VSITEMID_ROOT))
                    return Array.Empty<VSITEMSELECTION>();

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

        public static string? GetMkDocument(this VSITEMSELECTION selection)
        {
            ThrowIfNotOnUIThread();

            try
            {
                // ReSharper disable once SuspiciousTypeConversion.Global

                if (!(selection.pHier is IVsProject vsProject))
                    return null;

                vsProject.GetMkDocument(selection.itemid, out var itemFullPath);

                return itemFullPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IEnumerable<EnvDTE.ProjectItem>? TryGetProjectItems(this EnvDTE.ProjectItem? projectItem)
        {
            try
            {
                return projectItem?.ProjectItems?.OfType<EnvDTE.ProjectItem>().ToArray();
            }
            catch
            {
                return null;
            }
        }

        public static EnvDTE.ProjectItem? GetProjectItem(this EnvDTE.Document? document)
        {
            ThrowIfNotOnUIThread();

            try
            {
                return document?.ProjectItem;
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerable<EnvDTE.ProjectItem> Children(this EnvDTE.ProjectItem? projectItem)
        {
            return projectItem?.TryGetProjectItems() ?? Enumerable.Empty<EnvDTE.ProjectItem>();
        }

        public static IEnumerable<EnvDTE.ProjectItem> Descendants(this EnvDTE.ProjectItem? projectItem)
        {
            return projectItem?.TryGetProjectItems()?.SelectMany(p => p.DescendantsAndSelf()) ?? Enumerable.Empty<EnvDTE.ProjectItem>();
        }

        public static IEnumerable<EnvDTE.ProjectItem> DescendantsAndSelf(this EnvDTE.ProjectItem? projectItem)
        {
            if (projectItem == null)
                yield break;

            yield return projectItem;

            foreach (var item in projectItem.Descendants())
            {
                yield return item;
            }
        }

        public static void SetProperty(this EnvDTE.ProjectItem projectItem, string propertyName, object? value)
        {
            ThrowIfNotOnUIThread();

            var item = projectItem.Properties?.Item(propertyName);
            if (item != null)
                item.Value = value;
        }

        public static object? GetProperty(this EnvDTE.ProjectItem projectItem, string propertyName)
        {
            ThrowIfNotOnUIThread();

            try
            {
#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
                return projectItem.Properties?.OfType<EnvDTE.Property>()
                    .Where(p => p.Name == propertyName)
                    .Select(p => p.Value)
                    .FirstOrDefault();
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.
            }
            catch
            {
                return null;
            }
        }

        public static void RunCustomTool(this EnvDTE.ProjectItem projectItem)
        {
            ThrowIfNotOnUIThread();

            try
            {
                var vsProjectItem = projectItem.Object as VSLangProj.VSProjectItem;

                vsProjectItem?.RunCustomTool();
            }
            catch
            {
                // ignore
            }
        }

        public static void SetCustomTool(this EnvDTE.ProjectItem projectItem, string? value)
        {
            ThrowIfNotOnUIThread();

            SetProperty(projectItem, @"CustomTool", value);
        }

        public static string? GetCustomTool(this EnvDTE.ProjectItem projectItem)
        {
            ThrowIfNotOnUIThread();

            return GetProperty(projectItem, @"CustomTool") as string;
        }

        public static EnvDTE.ProjectItem? AddFromFile(this EnvDTE.ProjectItem projectItem, string? fileName)
        {
            ThrowIfNotOnUIThread();

            try
            {
                return projectItem.ProjectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static EnvDTE.ProjectItem? AddFromFile(this EnvDTE.Project project, string? fileName)
        {
            ThrowIfNotOnUIThread();

            try
            {
                return project.ProjectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SetFontSize(this EnvDTE80.DTE2 dte, DependencyObject view)
        {
            const string CATEGORY_FONTS_AND_COLORS = "FontsAndColors";
            const string PAGE_TEXT_EDITOR = "TextEditor";
            const string PROPERTY_FONT_SIZE = "FontSize";

            ThrowIfNotOnUIThread();

            try
            {
                var fontSizeObject = dte.Properties[CATEGORY_FONTS_AND_COLORS, PAGE_TEXT_EDITOR]?.Item(PROPERTY_FONT_SIZE)?.Value ?? 0.0;

                var fontSize = Convert.ToDouble(fontSizeObject, CultureInfo.InvariantCulture);

                if (fontSize > 1)
                {
                    // Default in VS is 10, but looks like 12 in WPF
                    view.SetValue(Appearance.TextFontSizeProperty, fontSize * 1.2);
                }
            }
            catch
            {
                // ignored
            }
        }

        public static string? TryGetFileName(this EnvDTE.ProjectItem projectItem)
        {
            ThrowIfNotOnUIThread();

            try
            {
                if (string.Equals(projectItem.Kind, ItemKind.PhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    // some items report a file count > 0 but don't return a file name!
                    return projectItem.FileNames[0];
                }
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

    }
}
