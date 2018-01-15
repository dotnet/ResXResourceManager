﻿namespace tomenglertde.ResXManager.VSIX
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
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;
    using tomenglertde.ResXManager.VSIX.Visuals;

    using Throttle;

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
        [NotNull]
        private readonly ICompositionHost _compositionHost = new CompositionHost();
        [NotNull]
        private readonly ManualResetEvent _compositionHostLoaded = new ManualResetEvent(false);

        [CanBeNull]
        private EnvDTE.SolutionEvents _solutionEvents;
        [CanBeNull]
        private EnvDTE.DocumentEvents _documentEvents;
        [CanBeNull]
        private EnvDTE.ProjectItemsEvents _projectItemsEvents;
        [CanBeNull]
        private EnvDTE.ProjectItemsEvents _solutionItemsEvents;
        [CanBeNull]
        private EnvDTE.ProjectItemsEvents _miscFilesEvents;
        [CanBeNull]
        private EnvDTE.ProjectsEvents _projectsEvents;

        [CanBeNull]
        private static VSPackage _instance;

        public VSPackage()
        {
            _instance = this;
        }

        [NotNull]
        public static VSPackage Instance
        {
            get
            {
                Contract.Ensures(Contract.Result<VSPackage>() != null);

                if (_instance == null)
                    throw new InvalidOperationException("Package is the entry point and is the first class to be created.");

                return _instance;
            }
        }

        [NotNull]
        public ICompositionHost CompositionHost
        {
            get
            {
                Contract.Ensures(Contract.Result<ICompositionHost>() != null);

                _compositionHostLoaded.WaitOne();

                return _compositionHost;
            }
        }

        [NotNull]
        private ITracer Tracer => CompositionHost.GetExportedValue<ITracer>();

        [NotNull]
        private PerformanceTracer PerformanceTracer => CompositionHost.GetExportedValue<PerformanceTracer>();

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var dispatcher = Dispatcher.CurrentDispatcher;

            ThreadPool.QueueUserWorkItem(_ => FillCatalog(dispatcher));

            ConnectEvents();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (!(GetService(typeof(IMenuCommandService)) is IMenuCommandService mcs))
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

        private static void ShowLoaderErrors([NotNull] ICompositionHost compositionHost, [NotNull][ItemNotNull] IList<string> loaderErrors)
        {
            Contract.Requires(compositionHost != null);
            Contract.Requires(loaderErrors != null);

            if (!loaderErrors.Any())
                return;

            var message = "Loader errors at start:\n" + string.Join("\n", loaderErrors);

            try
            {
                compositionHost.GetExportedValue<ITracer>().TraceError(message);
            }
            catch
            {
                MessageBox.Show(message);
            }
        }

        private void FillCatalog([NotNull] Dispatcher dispatcher)
        {
            Contract.Requires(dispatcher != null);

            var path = Path.GetDirectoryName(GetType().Assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(path));

            var errors = new List<string>();

            // var stopwatch = new Stopwatch();

            _compositionHost.Container.ComposeExportedValue(nameof(VSPackage), (IServiceProvider)this);

            foreach (var file in Directory.GetFiles(path, @"*.dll"))
            {
                Contract.Assume(!string.IsNullOrEmpty(file));

                if ("DocumentFormat.OpenXml.dll".Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    // stopwatch.Restart();
                    _compositionHost.AddCatalog(new AssemblyCatalog(file));
                    // errors.Add("Assembly: " + Path.GetFileName(file) + " => " + stopwatch.ElapsedMilliseconds);
                    // stopwatch.Stop();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    // ReSharper disable once PossibleNullReferenceException
                    errors.Add("Assembly: " + Path.GetFileName(file) + " => " + string.Join("\n", ex.LoaderExceptions.Select(l => l.Message + ": " + (l.InnerException?.Message ?? string.Empty))));
                }
                catch (Exception ex)
                {
                    errors.Add("Assembly: " + Path.GetFileName(file) + " => " + ex.Message);
                }
            }

            _compositionHostLoaded.Set();

            if (errors.Any())
            {
                dispatcher.BeginInvoke(() => ShowLoaderErrors(_compositionHost, errors));
            }
            else
            {
                dispatcher.BeginInvoke(() => ErrorProvider.Register(_compositionHost.Container));
            }
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
            _solutionEvents.Opened += Solution_Opened;
            _solutionEvents.AfterClosing += Solution_AfterClosing;

            _solutionEvents.ProjectAdded += Solution_ContentChanged;
            _solutionEvents.ProjectRemoved += Solution_ContentChanged;
            _solutionEvents.ProjectRenamed += (item, newName) => Solution_ContentChanged(item);

            _projectItemsEvents = events.ProjectItemsEvents;
            _projectItemsEvents.ItemAdded += Solution_ContentChanged;
            _projectItemsEvents.ItemRemoved += Solution_ContentChanged;
            _projectItemsEvents.ItemRenamed += (item, newName) => Solution_ContentChanged(item);

            _solutionItemsEvents = events.SolutionItemsEvents;
            _solutionItemsEvents.ItemAdded += Solution_ContentChanged;
            _solutionItemsEvents.ItemRemoved += Solution_ContentChanged;
            _solutionItemsEvents.ItemRenamed += (item, newName) => Solution_ContentChanged(item);

            _miscFilesEvents = events.MiscFilesEvents;
            _miscFilesEvents.ItemAdded += Solution_ContentChanged;
            _miscFilesEvents.ItemRemoved += Solution_ContentChanged;
            _miscFilesEvents.ItemRenamed += (item, newName) => Solution_ContentChanged(item);

            _projectsEvents = events.ProjectsEvents;
            _projectsEvents.ItemAdded += Solution_ContentChanged;
            _projectsEvents.ItemRemoved += Solution_ContentChanged;
            _projectsEvents.ItemRenamed += (item, newName) => Solution_ContentChanged(item);

            _documentEvents = events.DocumentEvents;
            _documentEvents.DocumentOpened += DocumentEvents_DocumentOpened;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
        }

        [NotNull]
        private static OleMenuCommand CreateMenuCommand([NotNull] IMenuCommandService mcs, int cmdId, [CanBeNull] EventHandler invokeHandler)
        {
            Contract.Requires(mcs != null);
            Contract.Ensures(Contract.Result<OleMenuCommand>() != null);

            var menuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, cmdId);
            var menuCommand = new OleMenuCommand(invokeHandler, menuCommandId);
            mcs.AddCommand(menuCommand);
            return menuCommand;
        }

        private void ShowToolWindow([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            ShowToolWindow();
        }

        [CanBeNull]
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

        internal bool ShowToolWindow()
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

        private void ShowSelectedResourceFiles([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            var selectedResourceEntites = GetSelectedResourceEntites()?.Distinct().ToArray();
            if (selectedResourceEntites == null)
                return;

            // if we open the window the first time, make sure it does not select all entities by default.
            Settings.Default.AreAllFilesSelected = false;

            var selectedEntities = CompositionHost.GetExportedValue<ResourceViewModel>().SelectedEntities;
            selectedEntities.Clear();
            selectedEntities.AddRange(selectedResourceEntites);

            ShowToolWindow();
        }

        private void SolutionExplorerContextMenuCommand_BeforeQueryStatus([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            if (!(sender is OleMenuCommand menuCommand))
                return;

            menuCommand.Text = Resources.OpenInResXManager;

            menuCommand.Visible = GetSelectedResourceEntites() != null;
        }

        [CanBeNull, ItemNotNull]
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
        [ItemNotNull]
        private IEnumerable<ResourceEntity> GetSelectedResourceEntites([CanBeNull] string fileName)
        {
            Contract.Ensures(Contract.Result<IEnumerable<ResourceEntity>>() != null);
            if (string.IsNullOrEmpty(fileName))
                return Enumerable.Empty<ResourceEntity>();

            var resourceEntities = CompositionHost.GetExportedValue<ResourceManager>().ResourceEntities;

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private static bool ContainsChildOfWinFormsDesignerItem([NotNull] ResourceEntity entity, [CanBeNull] string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName) && projectFile.IsWinFormsDesignerResource);
        }

        private static bool ContainsFile([NotNull] ResourceEntity entity, [CanBeNull] string fileName)
        {
            Contract.Requires(entity != null);

            return entity.Languages.Any(lang => lang.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private void MoveToResource([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            var entry = CompositionHost.GetExportedValue<IRefactorings>().MoveToResource(Dte.ActiveDocument);
            if (entry == null)
                return;

            // ReSharper disable once PossibleNullReferenceException
            if (!Properties.Settings.Default.MoveToResourceOpenInResXManager)
                return;

            CompositionHost.GetExportedValue<VsixShellViewModel>().SelectEntry(entry);
        }

        private void TextEditorContextMenuCommand_BeforeQueryStatus([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            if (!(sender is OleMenuCommand menuCommand))
                return;

            using (CompositionHost.GetExportedValue<PerformanceTracer>().Start("Can move to resource"))
            {
                menuCommand.Text = Resources.MoveToResource;
                menuCommand.Visible = CompositionHost.GetExportedValue<IRefactorings>().CanMoveToResource(Dte.ActiveDocument);
            }
        }

        private void Solution_Opened()
        {
            using (PerformanceTracer.Start("DTE event: Solution opened"))
            {
                ReloadSolution();

                var resourceManager = CompositionHost.GetExportedValue<ResourceManager>();

                resourceManager.ProjectFileSaved -= ResourceManager_ProjectFileSaved;
                resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;
            }
        }

        private void Solution_AfterClosing()
        {
            using (PerformanceTracer.Start("DTE event: Solution closed"))
            {
                ReloadSolution();
            }
        }

        private void Solution_ContentChanged([CanBeNull] object item)
        {
            using (PerformanceTracer.Start("DTE event: Solution content changed"))
            {
                CompositionHost.GetExportedValue<ISourceFilesProvider>().Invalidate();

                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentOpened([CanBeNull] EnvDTE.Document document)
        {
            using (PerformanceTracer.Start("DTE event: Document opened"))
            {
                if (!AffectsResourceFile(document))
                    return;

                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentSaved([NotNull] EnvDTE.Document document)
        {
            Contract.Requires(document != null);

            using (PerformanceTracer.Start("DTE event: Document saved"))
            {
                if (!AffectsResourceFile(document))
                    return;

                var resourceManager = CompositionHost.GetExportedValue<ResourceManager>();
                if (resourceManager.IsSaving)
                    return;

                // Run custom tool (usually attached to neutral language) even if a localized language changes,
                // e.g. if custom tool is a text template, we might want not only to generate the designer file but also
                // extract some localization information.
                // => find the resource entity that contains the document and run the custom tool on the neutral project file.

                // ReSharper disable once PossibleNullReferenceException
                bool Predicate(ResourceEntity e) => e.Languages.Select(lang => lang.ProjectFile)
                    .OfType<DteProjectFile>()
                    .Any(projectFile => projectFile.ProjectItems.Any(p => p.Document == document));

                var entity = resourceManager.ResourceEntities.FirstOrDefault(Predicate);

                var neutralProjectFile = (DteProjectFile)entity?.NeutralProjectFile;

                // VS will run the custom tool on the project item only. Run the custom tool on any of the descendants, too.
                var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.Descendants());

                _customToolRunner.Enqueue(projectItems);

                ReloadSolution();
            }
        }

        private void ResourceManager_ProjectFileSaved([NotNull] object sender, [NotNull] ProjectFileEventArgs e)
        {
            var entity = e.Language.Container;

            var neutralProjectFile = (DteProjectFile)entity.NeutralProjectFile;

            // VS will run the custom tool on the project item only if the document is open => Run the custom tool on any of the descendants, too.
            // VS will not run the custom tool if just the file is saved in background, and no document is open => Run the custom tool on all descendants and self.
            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.GetIsOpen() ? projectItem.Descendants() : projectItem.DescendantsAndSelf());

            _customToolRunner.Enqueue(projectItems);
        }

        private static bool AffectsResourceFile([CanBeNull] EnvDTE.Document document)
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

        [Throttled(typeof(DispatcherThrottle))]
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
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_compositionHostLoaded != null);
        }
    }
}
