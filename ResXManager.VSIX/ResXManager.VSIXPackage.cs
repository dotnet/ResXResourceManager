namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Threading;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Desktop.Composition;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(@"#110", @"#112", Product.Version, IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class ResXManagerVsixPackage : Package
    {
        private readonly CustomToolRunner _customToolRunner = new CustomToolRunner();

        private EnvDTE.DocumentEvents _documentEvents;

        private ICompositionHost _compositionHost;
        private ITracer _tracer;
        private IRefactorings _refactorings;
        private PerformanceTracer _performanceTracer;

        private EnvDTE80.DTE2 Dte
        {
            get
            {
                Contract.Ensures(Contract.Result<EnvDTE80.DTE2>() != null);

                var dte = (EnvDTE80.DTE2)GetService(typeof(SDTE));
                Contract.Assume(dte != null);
                return dte;
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            ConnectEvents();

            _compositionHost = ToolWindow?.CompositionHost;
            if (_compositionHost == null)
                return;

            _tracer = _compositionHost.GetExportedValue<ITracer>();
            _refactorings = _compositionHost.GetExportedValue<IRefactorings>();
            _performanceTracer = _compositionHost.GetExportedValue<PerformanceTracer>();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            if (null == mcs)
                return;

            // Create the command for the menu item.
            CreateMenuCommand(mcs, PkgCmdIdList.cmdidMyCommand, ShowToolWindow);
            // Create the command for the tool window
            CreateMenuCommand(mcs, PkgCmdIdList.cmdidMyTool, ShowToolWindow);
            // Create the command for the solution explorer context menu
            CreateMenuCommand(mcs, PkgCmdIdList.cmdidMySolutionExplorerContextMenu, ShowSelectedResourceFiles).BeforeQueryStatus += SolutionExplorerContextMenuCommand_BeforeQueryStatus;
            // Create the command for the text editor context menu
            CreateMenuCommand(mcs, PkgCmdIdList.cmdidMyTextEditorContextMenu, MoveToResource).BeforeQueryStatus += TextEditorContextMenuCommand_BeforeQueryStatus;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _customToolRunner.Dispose();
        }

        [ContractVerification(false)]
        private void ConnectEvents()
        {
            var events = (EnvDTE80.Events2)Dte.Events;

            _documentEvents = events.DocumentEvents;
            _documentEvents.DocumentOpened += DocumentEvents_DocumentOpened;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
        }

        private static OleMenuCommand CreateMenuCommand(IMenuCommandService mcs, int cmdId, EventHandler invokeHandler)
        {
            Contract.Requires(mcs != null);

            var menuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, cmdId);
            var menuCommand = new OleMenuCommand(invokeHandler, menuCommandId);
            mcs.AddCommand(menuCommand);
            return menuCommand;
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            ShowToolWindow();
        }

        // Get the instance number 0 of this tool window. This window is single instance so this instance is actually the only one.
        // The last flag is set to true so that if the tool window does not exists it will be created.
        private MyToolWindow ToolWindow => FindToolWindow();

        private MyToolWindow FindToolWindow()
        {
            try
            {
                return (MyToolWindow)FindToolWindow(typeof(MyToolWindow), 0, true);
            }
            catch (Exception ex)
            {
                _tracer.TraceError("FindToolWindow failed: " + ex);
                return null;
            }
        }

        private bool ShowToolWindow()
        {
            try
            {
                var window = ToolWindow;

                var windowFrame = (IVsWindowFrame)window?.Frame;
                if (windowFrame == null)
                    throw new NotSupportedException(Resources.CanNotCreateWindow);

                ErrorHandler.ThrowOnFailure(windowFrame.Show());
                return true;
            }
            catch (Exception ex)
            {
                _tracer.TraceError("ShowToolWindow failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
                return false;
            }
        }

        private void ShowSelectedResourceFiles(object sender, EventArgs e)
        {
            if (!ShowToolWindow())
                return;

            var resourceManager = ToolWindow?.ResourceManager;
            if (resourceManager == null)
                return;

            var selectedResourceEntites = GetSelectedResourceEntites()?.Distinct().ToArray();
            if (selectedResourceEntites == null)
                return;

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                resourceManager.SelectedEntities.Clear();
                resourceManager.SelectedEntities.AddRange(selectedResourceEntites);
            });
        }

        private void SolutionExplorerContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Text = Resources.OpenInResXManager;

            menuCommand.Visible = GetSelectedResourceEntites() != null;
        }

        private IEnumerable<ResourceEntity> GetSelectedResourceEntites()
        {
            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Contract.Assume(monitorSelection != null);

            var selection = monitorSelection.GetSelectedProjectItems();

            var entities = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !string.IsNullOrEmpty(file))
                .SelectMany(GetSelectedResourceEntites)
                .ToArray();

            return (entities.Length > 0) && (entities.Length == selection.Count) ? entities : null;
        }

        private IEnumerable<ResourceEntity> GetSelectedResourceEntites(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Enumerable.Empty<ResourceEntity>();

            var toolWindow = ToolWindow;
            if (toolWindow == null)
                return Enumerable.Empty<ResourceEntity>();

            var resourceEntities = toolWindow.ResourceManager.ResourceEntities;

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private static bool ContainsChildOfWinFormsDesignerItem(ResourceEntity entity, string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName) && projectFile.IsWinFormsDesignerResource);
        }

        private static bool ContainsFile(ResourceEntity entity, string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Any(lang => lang.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private void MoveToResource(object sender, EventArgs e)
        {
            var toolWindow = ToolWindow;
            if (toolWindow == null)
                return;

            var entry = _refactorings?.MoveToResource(Dte.ActiveDocument);
            if (entry == null)
                return;

            if (!Properties.Settings.Default.MoveToResourceOpenInResXManager)
                return;

            var dispatcher = Dispatcher.CurrentDispatcher;

            dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                ShowToolWindow();

                var resourceManager = toolWindow.ResourceManager;

                dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (!resourceManager.SelectedEntities.Contains(entry.Container))
                        resourceManager.SelectedEntities.Add(entry.Container);

                    resourceManager.SelectedTableEntries.Clear();
                    resourceManager.SelectedTableEntries.Add(entry);
                });
            });
        }

        private void TextEditorContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            using (_performanceTracer?.Start("Can move to resource"))
            {
                menuCommand.Text = Resources.MoveToResource;
                menuCommand.Visible = _refactorings?.CanMoveToResource(Dte.ActiveDocument) ?? false;
            }
        }

        private void DocumentEvents_DocumentOpened(EnvDTE.Document document)
        {
            _tracer?.WriteLine("DTE event: Document opened");

            if (!AffectsResourceFile(document))
                return;

            ToolWindow?.ReloadSolution();
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            _tracer?.WriteLine("DTE event: Document saved");

            if (!AffectsResourceFile(document))
                return;

            // Run custom tool (usually attached to neutral language) even if a localized language changes,
            // e.g. if custom tool is a text template, we might want not only to generate the designer file but also 
            // extract some localization information.
            // => find the resource entity that contains the document and run the custom tool on the neutral project file.
            Func<ResourceEntity, bool> predicate = e => e.Languages
                .Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => projectFile.ProjectItems.Any(p => p.Document == document));

            var entity = ToolWindow?.ResourceManager.ResourceEntities.FirstOrDefault(predicate);

            var neutralProjectFile = (DteProjectFile)entity?.NeutralProjectFile;

            // VS will run the custom tool on the project item only. Run the custom tool on any of the descendants, too.
            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.Descendants());

            _customToolRunner.Enqueue(projectItems);

            ToolWindow?.ReloadSolution();
        }

        private static bool AffectsResourceFile(EnvDTE.Document document)
        {
            Contract.Ensures((Contract.Result<bool>() == false) || (document != null));

            if (document == null)
                return false;

            return document.ProjectItem
                .DescendantsAndSelf()
                .Select(item => item.TryGetFileName())
                .Where(fileName => fileName != null)
                .Select(Path.GetExtension)
                .Any(extension => ProjectFileExtensions.SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_customToolRunner != null);
        }
    }
}
