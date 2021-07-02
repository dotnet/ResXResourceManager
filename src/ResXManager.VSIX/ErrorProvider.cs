namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Specialized;
    using System.Composition;
    using System.Linq;
    using Community.VisualStudio.Toolkit;
    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.VSIX.Visuals;

    using TomsToolbox.Composition;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;


    [Export]
    internal sealed class ErrorProvider : IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private readonly VsixShellViewModel _shellViewModel;
        private readonly DteConfiguration _configuration;
        private readonly ErrorListProvider _errorListProvider;
        private readonly TaskProvider.TaskCollection _tasks;

        [ImportingConstructor]
        public ErrorProvider(
            [Import(nameof(VsPackage))][Ninject.Named(nameof(VsPackage))] IServiceProvider serviceProvider,
            ResourceManager resourceManager,
            VsixShellViewModel shellViewModel,
            ITracer tracer,
            DteConfiguration configuration)
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

            _tasks = _errorListProvider.Tasks;

            VS.Events.BuildEvents.SolutionBuildStarted += BuildEvents_SolutionBuildStarted;
        }

        private void TableEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                TableEntries_CollectionChanged(e);
            }
            catch (MissingMethodException)
            {
                // BUG in VS2022: missing method Microsoft.VisualStudio.Shell.TaskProvider.TaskCollection.Remove
            }
        }

        private void TableEntries_CollectionChanged(NotifyCollectionChangedEventArgs e)
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

        private void BuildEvents_SolutionBuildStarted(object sender, EventArgs e)
        {
            try
            {
                BuildStarted();
            }
            catch (TypeLoadException)
            {
                // Bug in VS2022: : 'Could not load type 'Microsoft.VisualStudio.Shell.Task' from assembly 'Microsoft.VisualStudio.Shell.15.0, Version=17.0.0.0
            }
        }

        private void BuildStarted()
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

                        // Bug in VS2022: : this is the call that is responsible for the exeption: 'Could not load type 'Microsoft.VisualStudio.Shell.Task' from assembly 'Microsoft.VisualStudio.Shell.15.0, Version=17.0.0.0
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

        private void Task_Navigate(object? sender, EventArgs e)
        {
            ThrowIfNotOnUIThread();

            if (!(sender is ResourceErrorTask task))
                return;

            var entry = task.Entry;
            if (entry == null)
                return;

            _shellViewModel.SelectEntry(entry);
        }

        public void Dispose()
        {
            _errorListProvider.Dispose();

            VS.Events.BuildEvents.SolutionBuildStarted -= BuildEvents_SolutionBuildStarted;
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
