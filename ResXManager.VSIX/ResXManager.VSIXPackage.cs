namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;

    using EnvDTE;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

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
        private readonly DispatcherThrottle _deferedReloadThrottle;

        private EnvDTE.DTE _dte;
        private EnvDTE.DocumentEvents _documentEvents;
        private EnvDTE.SolutionEvents _solutionEvents;
        private EnvDTE.ProjectItemsEvents _solutionItemEvents;
        private MyToolWindow _toolWindow;

        public ResXManagerVsixPackage()
        {
            _deferedReloadThrottle = new DispatcherThrottle(DispatcherPriority.Background, () => FindToolWindow().ReloadSolution());
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            ConnectEvents();

            _toolWindow = FindToolWindow();

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

        [ContractVerification(false)]
        private void ConnectEvents()
        {
            _dte = (EnvDTE.DTE)GetService(typeof(SDTE));
            _documentEvents = _dte.Events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.Opened += AnyItem_Changed;
            _solutionEvents.ProjectAdded += _ => AnyItem_Changed();
            _solutionEvents.ProjectRemoved += _ => AnyItem_Changed();

            _solutionItemEvents = _dte.Events.SolutionItemsEvents;
            _solutionItemEvents.ItemAdded += _ => AnyItem_Changed();
            _solutionItemEvents.ItemRemoved += _ => AnyItem_Changed();
            _solutionItemEvents.ItemRenamed += (_,__) => AnyItem_Changed();
        }

        private void AnyItem_Changed()
        {
            _deferedReloadThrottle.Tick();
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

        private MyToolWindow FindToolWindow()
        {
            Contract.Ensures(Contract.Result<MyToolWindow>() != null);

            if (_toolWindow != null)
                return _toolWindow;

            // Get the instance number 0 of this tool window. This window is single instance so this instance is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(MyToolWindow), 0, true);

            if (window == null)
                throw new NotSupportedException(Resources.CanNotCreateWindow);

            return (MyToolWindow)window;
        }

        private MyToolWindow ShowToolWindow()
        {
            Contract.Ensures(Contract.Result<MyToolWindow>() != null);

            var window = FindToolWindow();

            var windowFrame = (IVsWindowFrame)window.Frame;
            if (windowFrame == null)
                throw new NotSupportedException(Resources.CanNotCreateWindow);

            ErrorHandler.ThrowOnFailure(windowFrame.Show());

            return window;
        }

        private void ShowSelectedResourceFiles(object sender, EventArgs e)
        {
            var toolWindow = ShowToolWindow();
            var resourceManager = toolWindow.ResourceManager;

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

            var toolWindow = FindToolWindow();
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
            var toolWindow = FindToolWindow();

            var entry = toolWindow.Refactorings.MoveToResource(_dte?.ActiveDocument);
            if (entry == null)
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

            menuCommand.Text = "Move to Resource";
            menuCommand.Visible = FindToolWindow().Refactorings.CanMoveToResource(_dte?.ActiveDocument);
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            if (!AffectsResourceFile(document))
                return;

            var toolWindow = FindToolWindow();

            if (toolWindow.ResourceManager.ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.ProjectFile.IsSaving))
                return;

            document.ProjectItem.Descendants().ForEach(projectItem => projectItem.RunCustomTool());

            toolWindow.ReloadSolution();
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
    }
}
