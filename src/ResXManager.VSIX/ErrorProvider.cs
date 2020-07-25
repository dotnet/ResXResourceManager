namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Specialized;
    using System.Composition;
    using System.Linq;

    using EnvDTE;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using ResXManager.Model;
    using ResXManager.VSIX.Visuals;

    using TomsToolbox.Composition;

    [Export]
    internal sealed class ErrorProvider : IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private readonly VsixShellViewModel _shellViewModel;
        private readonly DteConfiguration _configuration;
        private readonly ErrorListProvider _errorListProvider;
        private readonly TaskProvider.TaskCollection _tasks;

        private BuildEvents? _buildEvents;


        [ImportingConstructor]
        public ErrorProvider([Import(nameof(VsPackage))][Ninject.Named(nameof(VsPackage))]IServiceProvider serviceProvider, ResourceManager resourceManager, VsixShellViewModel shellViewModel, DteConfiguration configuration)
        {
            _resourceManager = resourceManager;
            _shellViewModel = shellViewModel;
            _configuration = configuration;

            resourceManager.TableEntries.CollectionChanged += TableEntries_CollectionChanged;

            _errorListProvider = new ErrorListProvider(serviceProvider)
            {
                ProviderName = "ResX Resource Manager",
                ProviderGuid = new Guid("36C23699-DA00-4D2C-8233-5484A091BFD2")
            };

            var tasks = _errorListProvider.Tasks;
            _tasks = tasks;

            var dte = (EnvDTE80.DTE2)serviceProvider.GetService(typeof(SDTE));
            var events = dte?.Events as EnvDTE80.Events2;
            var buildEvents = events?.BuildEvents;

            _buildEvents = buildEvents;

            if (buildEvents == null)
                return;

            buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
        }

        private void TableEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
                return;

            foreach (var removed in e.OldItems.OfType<ResourceTableEntry>())
            {
                var task = _tasks.OfType<ResourceErrorTask>().FirstOrDefault(t => t.Entry == removed);

                if (task == null)
                    continue;

                _tasks.Remove(task);
            }
        }

        public static void Register(IExportProvider exportProvider)
        {
            try
            {
                exportProvider.GetExportedValue<ErrorProvider>();
            }
            catch
            {
                // we had loader errors, ignore this... 
            }
        }

        private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            _errorListProvider.SuspendRefresh();

            try
            {
                _tasks.Clear();

                if (!_configuration.ShowErrorsInErrorList)
                    return;

                var errorCategory = _configuration.TaskErrorCategory;

                var cultures = _resourceManager.Cultures;

                var errorCount = 0;

                foreach (var entry in _resourceManager.TableEntries)
                {
                    foreach (var culture in cultures)
                    {
                        if (!entry.GetError(culture, out var error))
                            continue;

                        if (++errorCount >= 20)
                            return;

                        var task = new ResourceErrorTask(entry)
                        {
                            ErrorCategory = errorCategory,
                            Category = TaskCategory.BuildCompile,
                            Text = error,
                            Document = entry.Container.UniqueName,
                        };

                        task.Navigate += Task_Navigate;
                        _tasks.Add(task);
                    }
                }
            }
            finally
            {
                _errorListProvider.ResumeRefresh();
            }
        }

        private void Task_Navigate(object sender, EventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var task = (ResourceErrorTask)sender;
            var entry = task.Entry;

            if (entry == null)
                return;

            _shellViewModel.SelectEntry(entry);
        }

        public void Dispose()
        {
            _errorListProvider.Dispose();

            var buildEvents = _buildEvents;

            if (buildEvents == null)
                return;

            buildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;

            _buildEvents = null;
        }

        private class ResourceErrorTask : ErrorTask
        {
            public ResourceErrorTask(ResourceTableEntry entry)
            {
                Entry = entry;
            }

            public ResourceTableEntry? Entry { get; }
        }
    }
}
