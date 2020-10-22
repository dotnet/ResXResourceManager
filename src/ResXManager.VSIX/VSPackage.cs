namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using Ninject;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Tools;
    using ResXManager.View.Visuals;
    using ResXManager.VSIX.Visuals;

    using Throttle;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Ninject;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Package already handles this.")]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(@"#110", @"#112", Product.Version, IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VsPackage : AsyncPackage
    {
        private readonly CustomToolRunner _customToolRunner = new CustomToolRunner();
        private readonly IKernel _kernel = new StandardKernel();

        private EnvDTE.SolutionEvents? _solutionEvents;
        private EnvDTE.DocumentEvents? _documentEvents;
        private EnvDTE.ProjectItemsEvents? _projectItemsEvents;
        private EnvDTE.ProjectItemsEvents? _solutionItemsEvents;
        private EnvDTE.ProjectItemsEvents? _miscFilesEvents;
        private EnvDTE.ProjectsEvents? _projectsEvents;
        private PerformanceTracer? _performanceTracer;

        private static VsPackage? _instance;

        public VsPackage()
        {
            _instance = this;
            Tracer = new OutputWindowTracer(this);

            _kernel.Bind<ITracer>().ToConstant(Tracer);
            _kernel.Bind<IServiceProvider>().ToConstant(this).Named(nameof(VsPackage));
            ExportProvider = new ExportProvider(_kernel);
        }

        public static VsPackage Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("Package is the entry point and is the first class to be created.");

                return _instance;
            }
        }

        public IExportProvider ExportProvider { get; }

        private ITracer Tracer { get; }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            var loaderMessages = await System.Threading.Tasks.Task.Run(FillCatalog, cancellationToken).ConfigureAwait(false);

            _performanceTracer = ExportProvider.GetExportedValue<PerformanceTracer>();

            var menuCommandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(false);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            SynchronizationContextThrottle.TaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());

            ShowLoaderMessages(loaderMessages);

            ErrorProvider.Register(ExportProvider);

            ConnectEvents();

            // start background services
            ExportProvider
                .GetExportedValues<IService>()
                .ForEach(service => service.Start());

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (!(menuCommandService is IMenuCommandService mcs))
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
            _kernel.Dispose();
        }

        private void ShowLoaderMessages(LoaderMessages messages)
        {
            if (!messages.Errors.Any())
            {
                return;
            }

            try
            {
                foreach (var error in messages.Errors)
                {
                    Tracer.TraceError(error);
                }
                foreach (var message in messages.Messages)
                {
                    Tracer.WriteLine(message);
                }
            }
            catch
            {
                MessageBox.Show("Loader errors:\n" + string.Join("\n", messages.Errors));
            }
        }

        private LoaderMessages FillCatalog()
        {
            var assembly = GetType().Assembly;
            var messages = new LoaderMessages();

            //var allLocalAssemblyFileNames = Directory.EnumerateFiles(path, @"*.dll");
            //var allLocalAssemblyNames = new HashSet<string>(allLocalAssemblyFileNames.Select(Path.GetFileNameWithoutExtension));
            //var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            //var messages = loadedAssemblies
            //    .Where(a => allLocalAssemblyNames.Contains(a.GetName().Name))
            //    .Where(a => !string.Equals(Path.GetDirectoryName(a.Location), path, StringComparison.OrdinalIgnoreCase))
            //    .OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase)
            //    .Select(assembly => string.Format(CultureInfo.CurrentCulture, "Found assembly '{0}' already loaded from {1}.", assembly.FullName, assembly.CodeBase))
            //    .ToList();

            _kernel.BindExports(assembly,
                typeof(Infrastructure.Properties.AssemblyKey).Assembly,
                typeof(Model.Properties.AssemblyKey).Assembly,
                typeof(Translators.Properties.AssemblyKey).Assembly,
                typeof(View.Properties.AssemblyKey).Assembly);

            _kernel.Bind<IExportProvider>().ToConstant(ExportProvider);

            return messages;
        }

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

            if (Dte.Solution != null)
            {
                Solution_Opened();
            }
        }

        private static OleMenuCommand CreateMenuCommand(IMenuCommandService mcs, int cmdId, EventHandler? invokeHandler)
        {
            var menuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, cmdId);
            var menuCommand = new OleMenuCommand(invokeHandler, menuCommandId);
            mcs.AddCommand(menuCommand);
            return menuCommand;
        }

        private void ShowToolWindow(object? sender, EventArgs? e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            ShowToolWindow();
        }

        private MyToolWindow? FindToolWindow()
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
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var window = FindToolWindow();

                var windowFrame = (IVsWindowFrame?)window?.Frame;
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

        private void ShowSelectedResourceFiles(object? sender, EventArgs? e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var selectedResourceEntities = GetSelectedResourceEntities()?.Distinct().ToArray();
            if (selectedResourceEntities == null)
                return;

            // if we open the window the first time, make sure it does not select all entities by default.
            var settings = View.Properties.Settings.Default;
            settings.AreAllFilesSelected = false;
            settings.ResourceFilter = string.Empty;

            var selectedEntities = ExportProvider.GetExportedValue<ResourceViewModel>().SelectedEntities;
            selectedEntities.Clear();
            selectedEntities.AddRange(selectedResourceEntities);

            ShowToolWindow();
        }

        private void SolutionExplorerContextMenuCommand_BeforeQueryStatus(object? sender, EventArgs? e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (!(sender is OleMenuCommand menuCommand))
                return;

            menuCommand.Text = Resources.OpenInResXManager;
            menuCommand.Visible = GetSelectedResourceEntities() != null;
        }

        private IEnumerable<ResourceEntity>? GetSelectedResourceEntities()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            var selection = monitorSelection?.GetSelectedProjectItems();
            if (selection == null)
                return null;

            var entities = selection
                .Select(item => item.GetMkDocument())
                .Where(file => !file.IsNullOrEmpty())
                .SelectMany(GetSelectedResourceEntities)
                .ToArray();

            return (entities.Length > 0) && (entities.Length == selection.Count) ? entities : null;
        }

        private IEnumerable<ResourceEntity> GetSelectedResourceEntities(string? fileName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (fileName.IsNullOrEmpty())
                return Enumerable.Empty<ResourceEntity>();

            var resourceEntities = ExportProvider.GetExportedValue<ResourceManager>().ResourceEntities;

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private static bool ContainsChildOfWinFormsDesignerItem(ResourceEntity entity, string? fileName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return entity.Languages.Select(lang => lang.ProjectFile)
                .OfType<DteProjectFile>()
                .Any(projectFile => string.Equals(projectFile.ParentItem?.TryGetFileName(), fileName, StringComparison.OrdinalIgnoreCase) && projectFile.IsWinFormsDesignerResource);
        }

        private static bool ContainsFile(ResourceEntity entity, string? fileName)
        {
            return entity.Languages.Any(lang => lang.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void MoveToResource(object? sender, EventArgs? e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            FindToolWindow();

            if (!Microsoft.VisualStudio.Shell.ThreadHelper.CheckAccess())
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            try
            {
                var entry = await ExportProvider.GetExportedValue<IRefactorings>().MoveToResourceAsync(Dte.ActiveDocument).ConfigureAwait(true);
                if (entry == null)
                    return;

                if (!Properties.Settings.Default.MoveToResourceOpenInResXManager)
                    return;

                ExportProvider.GetExportedValue<VsixShellViewModel>().SelectEntry(entry);
            }
            catch (Exception ex)
            {
                Tracer.TraceError(ex.ToString());
            }
        }

        private void TextEditorContextMenuCommand_BeforeQueryStatus(object? sender, EventArgs? e)
        {
            if (!(sender is OleMenuCommand menuCommand))
                return;

            using (ExportProvider.GetExportedValue<PerformanceTracer>().Start("Can move to resource"))
            {
                menuCommand.Text = Resources.MoveToResource;
                menuCommand.Visible = ExportProvider.GetExportedValue<IRefactorings>().CanMoveToResource(Dte.ActiveDocument);
            }
        }

        private void Solution_Opened()
        {
            using (_performanceTracer?.Start("DTE event: Solution opened"))
            {
                ReloadSolution();

                var resourceManager = ExportProvider.GetExportedValue<ResourceManager>();

                resourceManager.ProjectFileSaved -= ResourceManager_ProjectFileSaved;
                resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;
            }
        }

        private void Solution_AfterClosing()
        {
            using (_performanceTracer?.Start("DTE event: Solution closed"))
            {
                Invalidate();
                ReloadSolution();
            }
        }

        private void Solution_ContentChanged(object? item)
        {
            using (_performanceTracer?.Start("DTE event: Solution content changed"))
            {
                Invalidate();
                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentOpened(EnvDTE.Document? document)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            using (_performanceTracer?.Start("DTE event: Document opened"))
            {
                if (!AffectsResourceFile(document))
                    return;

                ReloadSolution();
            }
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            using (_performanceTracer?.Start("DTE event: Document saved"))
            {
                if (!AffectsResourceFile(document))
                    return;

                var resourceManager = ExportProvider.GetExportedValue<ResourceManager>();
                if (resourceManager.IsSaving)
                    return;

                // Run custom tool (usually attached to neutral language) even if a localized language changes,
                // e.g. if custom tool is a text template, we might want not only to generate the designer file but also
                // extract some localization information.
                // => find the resource entity that contains the document and run the custom tool on the neutral project file.

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
                bool Predicate(ResourceEntity e) => e.Languages.Select(lang => lang.ProjectFile)
                    .OfType<DteProjectFile>()
                    .Any(projectFile => projectFile.ProjectItems.Any(p => p.Document == document));
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

                var entity = resourceManager.ResourceEntities.FirstOrDefault(Predicate);

                var neutralProjectFile = entity?.NeutralProjectFile as DteProjectFile;

                // VS will run the custom tool on the project item only. Run the custom tool on any of the descendants, too.
                var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.Descendants());

                _customToolRunner.Enqueue(projectItems);

                ReloadSolution();
            }
        }

        private void ResourceManager_ProjectFileSaved(object? sender, ProjectFileEventArgs e)
        {
            var entity = e.Language.Container;

            var neutralProjectFile = entity.NeutralProjectFile as DteProjectFile;

            // VS will run the custom tool on the project item only if the document is open => Run the custom tool on any of the descendants, too.
            // VS will not run the custom tool if just the file is saved in background, and no document is open => Run the custom tool on all descendants and self.
            var projectItems = neutralProjectFile?.ProjectItems.SelectMany(projectItem => projectItem.GetIsOpen() ? projectItem.Descendants() : projectItem.DescendantsAndSelf());

            _customToolRunner.Enqueue(projectItems);
        }

        private static bool AffectsResourceFile(EnvDTE.Document? document)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

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
            ExportProvider.GetExportedValue<ISourceFilesProvider>().Invalidate();
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.ContextIdle)]
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void ReloadSolution()
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await ExportProvider.GetExportedValue<ResourceViewModel>().ReloadAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Tracer.TraceError(ex.ToString());
            }
        }

        private class LoaderMessages
        {
            public IList<string> Messages { get; } = new List<string>();
            public IList<string> Errors { get; } = new List<string>();
        }
    }
}
