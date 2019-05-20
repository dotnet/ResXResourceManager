namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.View;

    internal static class DteExtensions
    {
        [CanBeNull]
        public static EnvDTE.Document TryGetDocument([CanBeNull] this EnvDTE.ProjectItem projectItem)
        {
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

        [CanBeNull]
        public static XDocument TryGetContent([NotNull] this EnvDTE.ProjectItem projectItem)
        {
            try
            {
                return !projectItem.IsOpen ? null : TryGetContent(projectItem.TryGetDocument());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool GetIsOpen([NotNull] this EnvDTE.ProjectItem projectItem)
        {
            try
            {
                return projectItem.IsOpen;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [NotNull]
        public static ICollection<VSITEMSELECTION> GetSelectedProjectItems([NotNull] this IVsMonitorSelection monitorSelection)
        {
            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out var itemId, out var multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr))
                    return new VSITEMSELECTION[0];

                if ((itemId == VSConstants.VSITEMID_SELECTION) && (multiItemSelect != null))
                {
                    multiItemSelect.GetSelectionInfo(out var cItems, out _);
                    var items = new VSITEMSELECTION[cItems];
                    multiItemSelect.GetSelectedItems(0, cItems, items);
                    return items;
                }

                if ((hierarchyPtr == IntPtr.Zero) || (itemId == VSConstants.VSITEMID_ROOT))
                    return new VSITEMSELECTION[0];

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

        [CanBeNull]
        public static string GetMkDocument(this VSITEMSELECTION selection)
        {
            try
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var vsProject = selection.pHier as IVsProject;

                if (vsProject == null)
                    return null;

                vsProject.GetMkDocument(selection.itemid, out var itemFullPath);

                return itemFullPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        private static XDocument TryGetContent([CanBeNull] EnvDTE.Document document)
        {
            try
            {
                var textDocument = (EnvDTE.TextDocument)document?.Object(@"TextDocument");
                var text = textDocument?.CreateEditPoint().GetText(textDocument.EndPoint);

                return text == null ? null : XDocument.Parse(text);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TrySetContent([NotNull] this EnvDTE.ProjectItem projectItem, [NotNull] XDocument value)
        {
            return projectItem.IsOpen && TrySetContent(projectItem.TryGetDocument(), value);
        }

        private static bool TrySetContent([CanBeNull] EnvDTE.Document document, [NotNull] XDocument value)
        {
            try
            {
                var textDocument = (EnvDTE.TextDocument)document?.Object(@"TextDocument");
                if (textDocument == null)
                    return false;

                var text = value.Declaration + Environment.NewLine + value;

                textDocument.CreateEditPoint().ReplaceText(textDocument.EndPoint, text, 0);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [CanBeNull, ItemNotNull]
        public static IEnumerable<EnvDTE.ProjectItem> TryGetProjectItems([CanBeNull] this EnvDTE.ProjectItem projectItem)
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

        [NotNull, ItemNotNull]
        public static IEnumerable<EnvDTE.ProjectItem> Children([CanBeNull] this EnvDTE.ProjectItem projectItem)
        {
            return projectItem?.TryGetProjectItems() ?? Enumerable.Empty<EnvDTE.ProjectItem>();
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<EnvDTE.ProjectItem> Descendants([CanBeNull] this EnvDTE.ProjectItem projectItem)
        {
            return projectItem?.TryGetProjectItems()?.SelectMany(p => p.DescendantsAndSelf()) ?? Enumerable.Empty<EnvDTE.ProjectItem>();
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<EnvDTE.ProjectItem> DescendantsAndSelf([CanBeNull] this EnvDTE.ProjectItem projectItem)
        {
            if (projectItem == null)
                yield break;

            yield return projectItem;

            foreach (var item in projectItem.Descendants())
            {
                yield return item;
            }
        }

        public static void SetProperty([NotNull] this EnvDTE.ProjectItem projectItem, [NotNull] string propertyName, [CanBeNull] object value)
        {
            var item = projectItem.Properties?.Item(propertyName);
            if (item != null)
                item.Value = value;
        }

        [CanBeNull]
        public static object GetProperty([NotNull] this EnvDTE.ProjectItem projectItem, [NotNull] string propertyName)
        {
            try
            {
                return projectItem.Properties?.OfType<EnvDTE.Property>()
                    .Where(p => p.Name == propertyName)
                    .Select(p => p.Value)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }


        public static void RunCustomTool([NotNull] this EnvDTE.ProjectItem projectItem)
        {
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

        public static void SetCustomTool([NotNull] this EnvDTE.ProjectItem projectItem, [CanBeNull] string value)
        {
            SetProperty(projectItem, @"CustomTool", value);
        }

        [CanBeNull]
        public static string GetCustomTool([NotNull] this EnvDTE.ProjectItem projectItem)
        {
            return GetProperty(projectItem, @"CustomTool") as string;
        }

        [CanBeNull]
        public static EnvDTE.ProjectItem AddFromFile([NotNull] this EnvDTE.ProjectItem projectItem, [CanBeNull] string fileName)
        {
            try
            {
                return projectItem.ProjectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        public static EnvDTE.ProjectItem AddFromFile([NotNull] this EnvDTE.Project project, [CanBeNull] string fileName)
        {
            try
            {
                return project.ProjectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void SetFontSize([NotNull] this EnvDTE.DTE dte, [NotNull] DependencyObject view)
        {
            const string CATEGORY_FONTS_AND_COLORS = "FontsAndColors";
            const string PAGE_TEXT_EDITOR = "TextEditor";
            const string PROPERTY_FONT_SIZE = "FontSize";

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

        [CanBeNull]
        public static string TryGetFileName([NotNull] this EnvDTE.ProjectItem projectItem)
        {
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
