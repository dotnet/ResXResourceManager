namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;

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
    [InstalledProductRegistration(@"#110", @"#112", @"1.0.0.64", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class ResXManagerVsixPackage : Package
    {
        private EnvDTE.DTE _dte;
        private EnvDTE.DocumentEvents _documentEvents;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            ConnectEvents();

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

        private MyToolWindow FindToolWindow(bool create)
        {
            Contract.Ensures((create == false) || (Contract.Result<MyToolWindow>() != null));

            // Get the instance number 0 of this tool window. This window is single instance so this instance is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(MyToolWindow), 0, create);

            if (create && (window == null))
                throw new NotSupportedException(Resources.CanNotCreateWindow);

            return (MyToolWindow)window;
        }

        private MyToolWindow ShowToolWindow()
        {
            Contract.Ensures(Contract.Result<MyToolWindow>() != null);

            var window = FindToolWindow(true);

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

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Contract.Assume(monitorSelection != null);

            var selection = monitorSelection.GetSelectedProjectItems();

            var selectedFiles = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !string.IsNullOrEmpty(file));

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                var selectedResourceEntites = resourceManager.ResourceEntities
                    .Where(entity => entity.Languages.Any(lang => selectedFiles.Any(file => lang.FileName.Equals(file, StringComparison.OrdinalIgnoreCase))));

                resourceManager.SelectedEntities.Clear();
                resourceManager.SelectedEntities.AddRange(selectedResourceEntites);
            });
        }

        private static void SolutionExplorerContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Text = Resources.OpenInResXManager;

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Contract.Assume(monitorSelection != null);

            var selection = monitorSelection.GetSelectedProjectItems();

            var selectedFiles = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !string.IsNullOrEmpty(file))
                .ToArray();

            menuCommand.Visible = selectedFiles.Any() && selectedFiles.All(file => ProjectFileExtensions.SupportedFileExtensions.Any(ext => Path.GetExtension(file).Equals(ext, StringComparison.OrdinalIgnoreCase)));
        }

        private void MoveToResource(object sender, EventArgs e)
        {
        }

        private static void TextEditorContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Text = "Move to Resource";
            menuCommand.Visible = false;
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            if (document == null)
                return;

            var extension = Path.GetExtension(document.Name);
            if (extension == null)
                return;

            if (!ProjectFileExtensions.SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return;

            var toolWindow = FindToolWindow(false);

            if ((toolWindow != null) && toolWindow.ResourceManager.ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.ProjectFile.IsSaving))
                return;

            document.ProjectItem.Descendants().ForEach(projectItem => projectItem.RunCustomTool());
        }
    }
}
