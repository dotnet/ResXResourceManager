namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Xml.Linq;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;

    internal static class DteExtensions
    {
        public static EnvDTE.Document TryGetDocument(this EnvDTE.ProjectItem projectItem)
        {
            try
            {
                return projectItem?.Document;
            }
            catch
            {
                return null;
            }
        }

        public static XDocument TryGetContent(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            try
            {
                return !projectItem.IsOpen ? null : TryGetContent(projectItem.TryGetDocument());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ICollection<VSITEMSELECTION> GetSelectedProjectItems(this IVsMonitorSelection monitorSelection)
        {
            Contract.Requires(monitorSelection != null);
            Contract.Ensures(Contract.Result<ICollection<VSITEMSELECTION>>() != null);

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                IVsMultiItemSelect multiItemSelect;
                uint itemId;

                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemId, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr))
                    return new VSITEMSELECTION[0];

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

        public static string GetMkDocument(this VSITEMSELECTION selection)
        {
            try
            {
                string itemFullPath;

                // ReSharper disable once SuspiciousTypeConversion.Global
                var vsProject = selection.pHier as IVsProject;

                if (vsProject == null)
                    return null;

                vsProject.GetMkDocument(selection.itemid, out itemFullPath);

                return itemFullPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [ContractVerification(false)]
        private static XDocument TryGetContent(EnvDTE.Document document)
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

        public static bool TrySetContent(this EnvDTE.ProjectItem projectItem, XDocument value)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(value != null);

            return projectItem.IsOpen && TrySetContent(projectItem.TryGetDocument(), value);
        }

        [ContractVerification(false)]
        private static bool TrySetContent(EnvDTE.Document document, XDocument value)
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

        public static IEnumerable<EnvDTE.ProjectItem> Children(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (projectItem == null)
                return Enumerable.Empty<EnvDTE.ProjectItem>();

            var projectItems = projectItem.ProjectItems;
            if (projectItems == null)
                return Enumerable.Empty<EnvDTE.ProjectItem>();

            return projectItems.OfType<EnvDTE.ProjectItem>();
        }

        public static IEnumerable<EnvDTE.ProjectItem> Descendants(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (projectItem == null)
                return Enumerable.Empty<EnvDTE.ProjectItem>();

            var projectItems = projectItem.ProjectItems;
            if (projectItems == null)
                return Enumerable.Empty<EnvDTE.ProjectItem>();

            return projectItem.ProjectItems.OfType<EnvDTE.ProjectItem>().SelectMany(p => p.DescendantsAndSelf());
        }

        public static IEnumerable<EnvDTE.ProjectItem> DescendantsAndSelf(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Ensures(Contract.Result<IEnumerable<EnvDTE.ProjectItem>>() != null);

            if (projectItem == null)
                yield break;

            yield return projectItem;

            foreach (var item in projectItem.Descendants())
            {
                yield return item;
            }
        }

        public static void SetProperty(this EnvDTE.ProjectItem projectItem, string propertyName, object value)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(propertyName != null);

            var item = projectItem.Properties?.Item(propertyName);
            if (item != null)
                item.Value = value;
        }

        public static object GetProperty(this EnvDTE.ProjectItem projectItem, string propertyName)
        {
            Contract.Requires(projectItem != null);
            Contract.Requires(propertyName != null);

            try
            {
                return projectItem.Properties?.Item(propertyName)?.Value;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }


        public static void RunCustomTool(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

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

        public static void SetCustomTool(this EnvDTE.ProjectItem projectItem, string value)
        {
            Contract.Requires(projectItem != null);

            SetProperty(projectItem, @"CustomTool", value);
        }

        public static string GetCustomTool(this EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            return GetProperty(projectItem, @"CustomTool") as string;
        }

        public static EnvDTE.ProjectItem AddFromFile(this EnvDTE.ProjectItem projectItem, string fileName)
        {
            Contract.Requires(projectItem != null);

            var projectItems = projectItem.ProjectItems;

            try
            {
                return projectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static EnvDTE.ProjectItem AddFromFile(this EnvDTE.Project project, string fileName)
        {
            Contract.Requires(project != null);

            var projectItems = project.ProjectItems;

            try
            {
                return projectItems?.AddFromFile(fileName);
            }
            catch (Exception)
            {
                return null;
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
                // ignored
            }
        }

        public static string TryGetFileName(this EnvDTE.ProjectItem projectItem)
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
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

    }
}
