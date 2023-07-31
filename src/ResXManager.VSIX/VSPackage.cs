namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    using Community.VisualStudio.Toolkit;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using Ninject;

    using Resourcer;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Tools;
    using ResXManager.View.Visuals;
    using ResXManager.VSIX.Compatibility;

    using Throttle;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Ninject;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    using Task = System.Threading.Tasks.Task;
    using MessageBox = System.Windows.MessageBox;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;
    using ResXManager.VSIX.Properties;


#pragma warning disable VSTHRD100 // Avoid async void methods

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Package already handles this.")]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute is used to register the information needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(@"#110", @"#112", "ResXManager", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(@"Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidResXManager_VSIXPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VsPackage : AsyncPackage
    {
        private readonly IKernel _kernel = new StandardKernel();

        private PerformanceTracer? _performanceTracer;
        private ResourceManager? _resourceManager;

        private static VsPackage? _instance;

        private bool _isToolWindowLoaded;
        private bool _isReloadRequestPending;

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

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            var loaderMessages = await Task.Run(FillCatalog, cancellationToken).ConfigureAwait(false);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _performanceTracer = ExportProvider.GetExportedValue<PerformanceTracer>();

            var menuCommandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);

            ShowLoaderMessages(loaderMessages);

            ConnectEvents();

            // start background services
            ExportProvider
                .GetExportedValues<IService>()
                .ForEach(service => service.Start());

            _resourceManager = ExportProvider.GetExportedValue<ResourceManager>();
            _resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (menuCommandService is not IMenuCommandService mcs)
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

            var is64BitProcess = Environment.Is64BitProcess;
            var isArmArchitecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            using var resourceStream = is64BitProcess
                ? (
                    isArmArchitecture
                    ? Resource.AsStream("ResXManager.VSIX.Compatibility.arm64.dll")
                    : Resource.AsStream("ResXManager.VSIX.Compatibility.x64.dll")
                    )
                : Resource.AsStream("ResXManager.VSIX.Compatibility.x86.dll");

            var length = resourceStream.Length;
            var data = new byte[length];
            resourceStream.Read(data, 0, (int)length);

            var compatibilityLayer = System.Reflection.Assembly.Load(data);

            _kernel.BindExports(assembly,
                typeof(Infrastructure.Properties.AssemblyKey).Assembly,
                typeof(Model.Properties.AssemblyKey).Assembly,
                typeof(Translators.Properties.AssemblyKey).Assembly,
                typeof(View.Properties.AssemblyKey).Assembly,
                compatibilityLayer);

            _kernel.Bind<IExportProvider>().ToConstant(ExportProvider);

            return messages;
        }

        private void ConnectEvents()
        {
            var events = VS.Events;

            var solutionEvents = events.SolutionEvents;
            solutionEvents.OnAfterOpenSolution += _ => Solution_Opened();
            solutionEvents.OnAfterCloseSolution += Solution_AfterClosing;

            solutionEvents.OnAfterOpenProject += _ => Solution_ContentChanged();
            solutionEvents.OnBeforeUnloadProject += _ => Solution_ContentChanged();
            solutionEvents.OnAfterRenameProject += _ => Solution_ContentChanged();

            Solution_Opened();
        }

        private static OleMenuCommand CreateMenuCommand(IMenuCommandService mcs, int cmdId, EventHandler invokeHandler)
        {
            var menuCommandId = new CommandID(GuidList.guidResXManager_VSIXCmdSet, cmdId);
            var menuCommand = new OleMenuCommand(invokeHandler, menuCommandId);
            mcs.AddCommand(menuCommand);
            return menuCommand;
        }

        private void ShowToolWindow(object? sender, EventArgs? e)
        {
            ThrowIfNotOnUIThread();

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
            ThrowIfNotOnUIThread();

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

        private async void ShowSelectedResourceFiles(object? sender, EventArgs? e)
        {
            try
            {
                var selectedResourceEntities = (await GetSelectedResourceEntitiesAsync().ConfigureAwait(false))?.Distinct().ToArray();
                if (selectedResourceEntities == null)
                    return;

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                // if we open the window the first time, make sure it does not select all entities by default.
                var settings = View.Properties.Settings.Default;
                settings.AreAllFilesSelected = false;
                settings.ResourceFilter = string.Empty;

                var selectedEntities = ExportProvider.GetExportedValue<ResourceViewModel>().SelectedEntities;
                selectedEntities.Clear();
                selectedEntities.AddRange(selectedResourceEntities);

                ShowToolWindow();
            }
            catch (Exception ex)
            {
                Tracer.TraceError("ShowSelectedResourceFiles failed: " + ex);
            }
        }

        private async void SolutionExplorerContextMenuCommand_BeforeQueryStatus(object? sender, EventArgs? e)
        {
            try
            {
                if (sender is not OleMenuCommand menuCommand)
                    return;

                menuCommand.Text = Resources.OpenInResXManager;
                menuCommand.Visible = await GetSelectedResourceEntitiesAsync().ConfigureAwait(true) != null;
            }
            catch (Exception ex)
            {
                Tracer.TraceError("SolutionExplorerContextMenuCommand_BeforeQueryStatus failed: " + ex);
            }
        }

        private async Task<IEnumerable<ResourceEntity>?> GetSelectedResourceEntitiesAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var vsixCompatibility = ExportProvider.GetExportedValue<IVsixCompatibility>();

            var selectedFiles = await vsixCompatibility.GetSelectedFilesAsync().ConfigureAwait(false);

            if (!selectedFiles.Any())
                return null;

            var resourceFiles = selectedFiles.Where(file => ProjectFileExtensions.IsResourceFile(file));

            var groups = await Task.WhenAll(resourceFiles.Select(GetSelectedResourceEntitiesAsync)).ConfigureAwait(true);

            var entities = groups
                .SelectMany(items => items)
                .ToArray();

            return (entities.Length > 0) && (entities.Length == selectedFiles.Count) ? entities : null;
        }

        private async Task<IEnumerable<ResourceEntity>> GetSelectedResourceEntitiesAsync(string fileName)
        {
            var resourceEntities = await GetResourceEntitiesAsync().ConfigureAwait(false);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var vsixCompatibility = ExportProvider.GetExportedValue<IVsixCompatibility>();

            return resourceEntities
                .Where(entity => ContainsFile(entity, fileName) || vsixCompatibility.ContainsChildOfWinFormsDesignerItem(entity, fileName))
                .ToArray();
        }

        private async Task<IEnumerable<ResourceEntity>> GetResourceEntitiesAsync()
        {
            await EnsureLoadedAsync().ConfigureAwait(false);

            var resourceEntities = _resourceManager?.ResourceEntities;

            return resourceEntities?.AsEnumerable() ?? Array.Empty<ResourceEntity>();
        }

        private async Task EnsureLoadedAsync()
        {
            if (_isReloadRequestPending)
            {
                await ExportProvider.GetExportedValue<ResourceViewModel>().ReloadAsync().ConfigureAwait(false);
                _isReloadRequestPending = false;
            }
        }

        private static bool ContainsFile(ResourceEntity entity, string? fileName)
        {
            return entity.Languages.Any(lang => lang.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private async void MoveToResource(object? sender, EventArgs? e)
        {
            await EnsureLoadedAsync().ConfigureAwait(false);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var entry = await ExportProvider.GetExportedValue<IRefactorings>().MoveToResourceAsync().ConfigureAwait(true);
                if (entry == null)
                    return;

                if (!Properties.Settings.Default.MoveToResourceOpenInResXManager)
                    return;

                ShowToolWindow();

                ExportProvider.GetExportedValue<IVsixShellViewModel>().SelectEntry(entry);
            }
            catch (Exception ex)
            {
                Tracer.TraceError(ex.ToString());
            }
        }

        private void TextEditorContextMenuCommand_BeforeQueryStatus(object? sender, EventArgs? e)
        {
            if (sender is not OleMenuCommand menuCommand)
                return;

            using (ExportProvider.GetExportedValue<PerformanceTracer>().Start("Can move to resource"))
            {
                menuCommand.Text = Resources.MoveToResource;
                menuCommand.Visible = ExportProvider.GetExportedValue<IRefactorings>().CanMoveToResource();
            }
        }

        private void Solution_Opened()
        {
            using (_performanceTracer?.Start("DTE event: Solution opened"))
            {
                ReloadSolution();
            }
        }

        private async void Solution_AfterClosing()
        {
            try
            {
                var resourceManager = _resourceManager;
                if (resourceManager == null)
                    return;

                if (resourceManager.HasChanges)
                {
                    if (await VS.MessageBox.ShowAsync(View.Properties.Resources.Title, Resources.QuerySaveUnchangedResources, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_YESNO).ConfigureAwait(true) == VSConstants.MessageBoxResult.IDYES)
                    {
                        resourceManager.Save();
                    }
                }

                using (_performanceTracer?.Start("DTE event: Solution closed"))
                {
                    await resourceManager.ClearAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError(ex.ToString());
            }
        }

        private void Solution_ContentChanged()
        {
            using (_performanceTracer?.Start("DTE event: Solution content changed"))
            {
                ReloadSolution();
            }
        }

        private void ResourceManager_ProjectFileSaved(object? sender, ProjectFileEventArgs e)
        {
            var entity = e.Language.Container;

            var vsixCompatibility = ExportProvider.GetExportedValue<IVsixCompatibility>();
            vsixCompatibility.RunCustomTool(entity);
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.ContextIdle)]
        private async void ReloadSolution()
        {
            try
            {
                if (!_isToolWindowLoaded)
                {
                    _isReloadRequestPending = true;
                    return;
                }

                await ExportProvider.GetExportedValue<ResourceViewModel>().ReloadAsync().ConfigureAwait(false);
                _isReloadRequestPending = false;
            }
            catch (Exception ex)
            {
                Tracer.TraceError(ex.ToString());
            }
        }

        private sealed class LoaderMessages
        {
            public IList<string> Messages { get; } = new List<string>();
            public IList<string> Errors { get; } = new List<string>();
        }

        public async void ToolWindowLoaded()
        {
            _isToolWindowLoaded = true;

            await EnsureLoadedAsync().ConfigureAwait(false);
        }

        public void ToolWindowUnloaded()
        {
            _isToolWindowLoaded = false;
        }
    }
}
