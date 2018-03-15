namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Linq;

    using EnvDTE;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.VSIX.Visuals;

    [Export]
    internal sealed class ErrorProvider : IDisposable
    {
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly VsixShellViewModel _shellViewModel;
        [NotNull]
        private readonly DteConfiguration _configuration;
        [NotNull]
        private readonly ErrorListProvider _errorListProvider;
        [NotNull]
        private readonly TaskProvider.TaskCollection _tasks;

        private BuildEvents _buildEvents;


        [ImportingConstructor]
        public ErrorProvider([Import(nameof(VSPackage))][NotNull] IServiceProvider serviceProvider, [NotNull] ResourceManager resourceManager, [NotNull] VsixShellViewModel shellViewModel, [NotNull] DteConfiguration configuration)
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
            Debug.Assert(tasks != null, nameof(tasks) + " != null");
            _tasks = tasks;

            var dte = (EnvDTE80.DTE2)serviceProvider.GetService(typeof(SDTE));
            var events = (EnvDTE80.Events2)dte?.Events;
            var buildEvents = events?.BuildEvents;

            _buildEvents = buildEvents;

            if (buildEvents == null)
                return;

            buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
        }

        private void TableEntries_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
                return;

            // ReSharper disable once AssignNullToNotNullAttribute
            foreach (var removed in e.OldItems.OfType<ResourceTableEntry>())
            {
                var task = _tasks.OfType<ResourceErrorTask>().FirstOrDefault(t => t.Entry == removed);

                if (task == null)
                    continue;

                _tasks.Remove(task);
            }
        }

        public static void Register([NotNull] ExportProvider exportProvider)
        {
            exportProvider.GetExportedValue<ErrorProvider>();
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

        private void Task_Navigate([NotNull] object sender, EventArgs e)
        {
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

            public ResourceTableEntry Entry { get; }
        }
    }
}
