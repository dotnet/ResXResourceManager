namespace tomenglertde.ResXManager.VSIX
{
    using System;
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

    using VSLangProj;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(@"#110", @"#112", @"1.0.0.61", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class ResXManagerVsixPackage : Package
    {
        private DTE _dte;
        private DocumentEvents _documentEvents;

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
            var menuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, PkgCmdIdList.cmdidMyCommand);
            var menuCommand = new MenuCommand(ShowToolWindow, menuCommandId);
            mcs.AddCommand(menuCommand);

            // Create the command for the tool window
            var toolWindowCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, PkgCmdIdList.cmdidMyTool);
            var toolWindowCommand = new MenuCommand(ShowToolWindow, toolWindowCommandId);
            mcs.AddCommand(toolWindowCommand);

            // Create the command for the solution explorer context menu
            var contextMenuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, PkgCmdIdList.cmdidMyContextMenu);
            var contextMenuCommand = new OleMenuCommand(ShowSelectedResourceFiles, contextMenuCommandId);
            contextMenuCommand.BeforeQueryStatus += ContextMenuCommand_BeforeQueryStatus;
            mcs.AddCommand(contextMenuCommand);
        }

        [ContractVerification(false)]
        private void ConnectEvents()
        {
            _dte = (DTE) GetService(typeof (SDTE));
            _documentEvents = _dte.Events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
        }

        private void DocumentEvents_DocumentSaved(Document document)
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

            document.ProjectItem.Descendants()
                .Select(projectItem => projectItem.Object)
                .OfType<VSProjectItem>()
                .ForEach(vsProjectItem => vsProjectItem.RunCustomTool());
        }

        private static void ContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Contract.Assume(monitorSelection != null);

            var selection = monitorSelection.GetSelectedProjectItems();

            var selectedFiles = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !string.IsNullOrEmpty(file))
                .ToArray();

            menuCommand.Visible = selectedFiles.Any() && selectedFiles.All(file => ProjectFileExtensions.SupportedFileExtensions.Any(ext => Path.GetExtension(file).Equals(ext, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
