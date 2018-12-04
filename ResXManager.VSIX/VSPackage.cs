namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
            Tracer = new OutputWindowTracer(this);
        }

        [NotNull]
        public static VSPackage Instance
        {
            get
            {
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
                var stopwatch = Stopwatch.StartNew();

                _compositionHostLoaded.WaitOne();

                stopwatch.Stop();

                if (stopwatch.Elapsed >= TimeSpan.FromMilliseconds(100))
                {
                    Tracer.WriteLine("Init: " + stopwatch.ElapsedMilliseconds + " ms");
                }

                return _compositionHost;
            }
        }

        [NotNull]
        private ITracer Tracer { get; }

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

        private void ShowLoaderMessages([NotNull, ItemNotNull] IList<string> errors, [NotNull, ItemNotNull] IList<string> messages)
        {
            if (!errors.Any())
            {
                return;
            }

            try
            {
                foreach (var error in errors)
                {
                    Tracer.TraceError(error);
                }
                foreach (var message in messages)
                {
                    Tracer.WriteLine(message);
                }
            }
            catch
            {
                MessageBox.Show("Loader errors:\n" + string.Join("\n", errors));
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private void FillCatalog([NotNull] Dispatcher dispatcher)
        {
            var compositionContainer = _compositionHost.Container;

            compositionContainer.ComposeExportedValue(nameof(VSPackage), (IServiceProvider)this);
            compositionContainer.ComposeExportedValue(Tracer);

            var thisAssembly = GetType().Assembly;

            var path = Path.GetDirectoryName(thisAssembly.Location);

            var messages = new List<string>();

            //var allLocalAssemblyFileNames = Directory.EnumerateFiles(path, @"*.dll");
            //var allLocalAssemblyNames = new HashSet<string>(allLocalAssemblyFileNames.Select(Path.GetFileNameWithoutExtension));
            //var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            //var messages = loadedAssemblies
            //    .Where(a => allLocalAssemblyNames.Contains(a.GetName().Name))
            //    .Where(a => !string.Equals(Path.GetDirectoryName(a.Location), path, StringComparison.OrdinalIgnoreCase))
            //    .OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase)
            //    .Select(assembly => string.Format(CultureInfo.CurrentCulture, "Found assembly '{0}' already loaded from {1}.", assembly.FullName, assembly.CodeBase))
            //    .ToList();

            var errors = new List<string>();

            foreach (var file in Directory.EnumerateFiles(path, @"ResXManager.*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    messages.Add(string.Format(CultureInfo.CurrentCulture, "Loaded assembly '{0}' from {1}.", assembly.FullName, assembly.CodeBase));
                    _compositionHost.AddCatalog(new AssemblyCatalog(assembly));
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

            dispatcher.BeginInvoke(() => ShowLoaderMessages(errors, messages));
            dispatcher.BeginInvoke(() => ErrorProvider.Register(compositionContainer));
        }

        [NotNull]
        private EnvDTE80.DTE2 Dte
        {
            get
            {
                var dte = (EnvDTE80.DTE2)GetService(typeof(SDTE));
                return dte;
            }
        }

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
            var selectedResourceEntities = GetSelectedResourceEntities()?.Distinct().ToArray();
            if (selectedResourceEntities == null)
                return;

            // if we open the window the first time, make sure it does not select all entities by default.
            Settings.Default.AreAllFilesSelected = false;

            var selectedEntities = CompositionHost.GetExportedValue<ResourceViewModel>().SelectedEntities;
            selectedEntities.Clear();
            selectedEntities.AddRange(selectedResourceEntities);

            ShowToolWindow();
        }

        private void SolutionExplorerContextMenuCommand_BeforeQueryStatus([CanBeNull] object sender, [CanBeNull] EventArgs e)
        {
            if (!(sender is OleMenuCommand menuCommand))
                return;

            menuCommand.Text = Resources.OpenInResXManager;

            menuCommand.Visible = GetSelectedResourceEntities() != null;
        }

        [CanBeNull, ItemNotNull]
        private IEnumerable<ResourceEntity> GetSelectedResourceEntities()
        {
            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            var selection = monitorSelection.GetSelectedProjectItems();

            var entities = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !string.IsNullOrEmpty(file))
                .SelectMany(GetSelectedResourceEntities)
                .ToArray();

            return (entities.Length > 0) && (entities.Length == selection.Count) ? entities : null;
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<ResourceEntity> GetSelectedResourceEntities([CanBeNull] string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Enumerable.Empty<ResourceEntity>();

            var resourceEntities = CompositionHost.GetExportedValue<ResourceManager>().ResourceEntities;

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private static bool ContainsChildOfWinFormsDesignerItem([NotNull] ResourceEntity entity, [CanBeNull] string fileName)
        {
            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName) && projectFile.IsWinFormsDesignerResource);
        }

        private static bool ContainsFile([NotNull] ResourceEntity entity, [CanBeNull] string fileName)
        {
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
            //using (PerformanceTracer.Start("DTE event: Solution opened"))
            {
                ReloadSolution();

                var resourceManager = CompositionHost.GetExportedValue<ResourceManager>();

                resourceManager.ProjectFileSaved -= ResourceManager_ProjectFileSaved;
                resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;
            }
        }

        private void Solution_AfterClosing()
        {
            //using (PerformanceTracer.Start("DTE event: Solution closed"))
            {
                Invalidate();
                ReloadSolution();
            }
        }

        private void Solution_ContentChanged([CanBeNull] object item)
        {
            //using (PerformanceTracer.Start("DTE event: Solution content changed"))
            {
                Invalidate();
                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentOpened([CanBeNull] EnvDTE.Document document)
        {
            //using (PerformanceTracer.Start("DTE event: Document opened"))
            {
                if (!AffectsResourceFile(document))
                    return;

                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentSaved([NotNull] EnvDTE.Document document)
        {
            //using (PerformanceTracer.Start("DTE event: Document saved"))
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
            if (document == null)
                return false;

            return document.ProjectItem
                .DescendantsAndSelf()
                .Select(item => item.TryGetFileName())
                .Where(fileName => fileName != null)
                .Select(Path.GetExtension)
                .Any(extension => ProjectFileExtensions.SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Background)]
        private void Invalidate()
        {
            CompositionHost.GetExportedValue<ISourceFilesProvider>().Invalidate();
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.ContextIdle)]
        private void ReloadSolution()
        {
            CompositionHost.GetExportedValue<ResourceViewModel>().Reload();
        }
    }
}
