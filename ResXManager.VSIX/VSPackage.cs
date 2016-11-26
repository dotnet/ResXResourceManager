namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Desktop.Composition;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Package already handles this.")]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(@"#110", @"#112", Product.Version, IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class VSPackage : Package
    {
        [NotNull]
        private readonly CustomToolRunner _customToolRunner = new CustomToolRunner();

        private EnvDTE.SolutionEvents _solutionEvents;
        private EnvDTE.DocumentEvents _documentEvents;

        public VSPackage()
        {
            Instance = this;
        }

        [NotNull]
        public static VSPackage Instance { get; private set; }

        [NotNull]
        public ICompositionHost CompositionHost { get; } = new CompositionHost();

        [NotNull]
        private ITracer Tracer => CompositionHost.GetExportedValue<ITracer>();

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var path = Path.GetDirectoryName(GetType().Assembly.Location);
            Contract.Assume(path != null);

            CompositionHost.AddCatalog(new DirectoryCatalog(path, @"*.dll"));
            CompositionHost.Container.ComposeExportedValue(nameof(VSPackage), (IServiceProvider)this);

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _customToolRunner.Dispose();
            CompositionHost.Dispose();
        }

        [NotNull]
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
       
        [ContractVerification(false)]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void ConnectEvents()
        {
            var events = (EnvDTE80.Events2)Dte.Events;

            _solutionEvents = events.SolutionEvents;
            _solutionEvents.Opened += SolutionEvents_Opened;
            _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;

            _documentEvents = events.DocumentEvents;
            _documentEvents.DocumentOpened += DocumentEvents_DocumentOpened;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
        }

        [NotNull]
        private static OleMenuCommand CreateMenuCommand([NotNull] IMenuCommandService mcs, int cmdId, EventHandler invokeHandler)
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
            try
            {
                return (MyToolWindow)FindToolWindow(typeof(MyToolWindow), 0, true);
            }
            catch (Exception ex)
            {
                Tracer.TraceError("FindToolWindow failed: " + ex);
                return null;
            }
        }

        private bool ShowToolWindow()
        {
            try
            {
                var window = FindToolWindow();

                var windowFrame = (IVsWindowFrame)window?.Frame;
                if (windowFrame == null)
                    throw new NotSupportedException(Resources.CanNotCreateWindow);

                ErrorHandler.ThrowOnFailure(windowFrame.Show());
                return true;
            }
            catch (Exception ex)
            {
                Tracer.TraceError("ShowToolWindow failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
                return false;
            }
        }

        private void ShowSelectedResourceFiles(object sender, EventArgs e)
        {
            if (!ShowToolWindow())
                return;

            var resourceViewModel = CompositionHost.GetExportedValue<ResourceViewModel>();

            var selectedResourceEntites = GetSelectedResourceEntites()?.Distinct().ToArray();
            if (selectedResourceEntites == null)
                return;

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                resourceViewModel.SelectedEntities.Clear();
                resourceViewModel.SelectedEntities.AddRange(selectedResourceEntites);
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

        [NotNull]
        private IEnumerable<ResourceEntity> GetSelectedResourceEntites(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Enumerable.Empty<ResourceEntity>();

            var resourceEntities = CompositionHost.GetExportedValue<ResourceManager>().ResourceEntities;

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private static bool ContainsChildOfWinFormsDesignerItem([NotNull] ResourceEntity entity, string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName) && projectFile.IsWinFormsDesignerResource);
        }

        private static bool ContainsFile([NotNull] ResourceEntity entity, string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Any(lang => lang.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private void MoveToResource(object sender, EventArgs e)
        {
            var entry = CompositionHost.GetExportedValue<IRefactorings>().MoveToResource(Dte.ActiveDocument);
            if (entry == null)
                return;

            if (!Properties.Settings.Default.MoveToResourceOpenInResXManager)
                return;

            var dispatcher = Dispatcher.CurrentDispatcher;

            dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                ShowToolWindow();

                var resourceViewModel = CompositionHost.GetExportedValue<ResourceViewModel>();

                dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (!resourceViewModel.SelectedEntities.Contains(entry.Container))
                        resourceViewModel.SelectedEntities.Add(entry.Container);

                    resourceViewModel.SelectedTableEntries.Clear();
                    resourceViewModel.SelectedTableEntries.Add(entry);
                });
            });
        }

        private void TextEditorContextMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            using (CompositionHost.GetExportedValue<PerformanceTracer>().Start("Can move to resource"))
            {
                menuCommand.Text = Resources.MoveToResource;
                menuCommand.Visible = CompositionHost.GetExportedValue<IRefactorings>().CanMoveToResource(Dte.ActiveDocument);
            }
        }

        private void SolutionEvents_Opened()
        {
            Tracer.WriteLine("DTE event: Solution opened");

            ReloadSolution();
        }

        private void SolutionEvents_AfterClosing()
        {
            Tracer.WriteLine("DTE event: Solution closed");

            ReloadSolution();
        }

        private void DocumentEvents_DocumentOpened(EnvDTE.Document document)
        {
            Tracer.WriteLine("DTE event: Document opened");

            if (!AffectsResourceFile(document))
                return;

            ReloadSolution();
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            Tracer.WriteLine("DTE event: Document saved");

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

            var entity = CompositionHost.GetExportedValue<ResourceManager>().ResourceEntities.FirstOrDefault(predicate);

            var neutralProjectFile = (DteProjectFile)entity?.NeutralProjectFile;

            // VS will run the custom tool on the project item only. Run the custom tool on any of the descendants, too.
            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.Descendants());

            _customToolRunner.Enqueue(projectItems);

            ReloadSolution();
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

        private void ReloadSolution()
        {
            CompositionHost.GetExportedValue<ResourceViewModel>().Reload();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_customToolRunner != null);
        }
    }
}
