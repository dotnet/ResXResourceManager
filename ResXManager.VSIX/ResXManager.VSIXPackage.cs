namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;

    using EnvDTE;
    using EnvDTE80;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TextManager.Interop;

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
    [InstalledProductRegistration(@"#110", @"#112", @"1.0.0.63", IconResourceID = 400)]
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
            _dte = (DTE)GetService(typeof(SDTE));
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
            var _textManager = (IVsTextManager)GetGlobalService(typeof(SVsTextManager));
            if (_textManager == null)
                throw new Exception("Cannot consume IVsTextManager service");

            IVsTextView _textView;
            var hr = _textManager.GetActiveView(1, null, out _textView);
            Marshal.ThrowExceptionForHR(hr);

            IVsTextLines _textLines;
            hr = _textView.GetBuffer(out _textLines);
            Marshal.ThrowExceptionForHR(hr);

            //hr = _textLines.GetUndoManager(out _undoManager);
            //Marshal.ThrowExceptionForHR(hr);           
            TextSpan[] spans = new TextSpan[1];
            hr = _textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            var selectionSpan = spans[0];

            object startPoint;
            hr = _textLines.CreateTextPoint(selectionSpan.iStartLine, 0, out startPoint);
            var textPoint = (TextPoint)startPoint;

            string text;
            _textLines.GetLineText(selectionSpan.iStartLine, 0, selectionSpan.iStartLine, textPoint.LineLength, out text);

            Marshal.ThrowExceptionForHR(hr);
            TextPoint selectionPoint = (TextPoint)startPoint;

            var _currentDocument = _dte.ActiveDocument;

            if (_currentDocument == null)
                throw new Exception("No selected document");
            if (_currentDocument.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");

            var _currentCodeModel = _currentDocument.ProjectItem.FileCodeModel;

            foreach (var x in _currentCodeModel.CodeElements.OfType<CodeElement2>())
            {
                Inspect(x);
            }

            CodeFunction2 codeFunction = (CodeFunction2)_currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementFunction);


        }

        private void Inspect(CodeElement2 x)
        {
            Debug.WriteLine(x.Kind);

            foreach (var item in x.Children.OfType<CodeElement2>())
            {
                Debug.Indent();
                Inspect(item);
                Debug.Unindent();
            }
        }

        private static void TextEditorContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Text = "Move to Resource";
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
    }
}
