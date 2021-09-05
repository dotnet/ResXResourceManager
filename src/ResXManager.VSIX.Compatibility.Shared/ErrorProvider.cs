namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Specialized;
    using System.Composition;
    using System.Linq;

    using Community.VisualStudio.Toolkit;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.VSIX.Compatibility;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Shared]
    [Export(typeof(IService))]
    internal sealed class ErrorProvider : IService, IDisposable
    {
        private readonly IErrorListProvider _errorListProvider;
        private readonly ResourceManager _resourceManager;
        private readonly IVsixShellViewModel _shellViewModel;
        private readonly IDteConfiguration _configuration;

        [ImportingConstructor]
        public ErrorProvider(
            ResourceManager resourceManager,
            IVsixShellViewModel shellViewModel,
            IErrorListProvider errorListProvider,
            IDteConfiguration configuration)
        {
            _errorListProvider = errorListProvider;
            _resourceManager = resourceManager;
            _shellViewModel = shellViewModel;
            _configuration = configuration;

            resourceManager.TableEntries.CollectionChanged += TableEntries_CollectionChanged;
            errorListProvider.Navigate += Provider_Navigate;

            VS.Events.BuildEvents.SolutionBuildStarted += BuildEvents_SolutionBuildStarted;
        }

        public void Start()
        {
        }

        private void TableEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
                return;

            foreach (var entry in e.OldItems.OfType<ResourceTableEntry>())
            {
                _errorListProvider.Remove(entry);
            }
        }

        private void BuildEvents_SolutionBuildStarted(object sender, EventArgs e)
        {
            if (!_configuration.ShowErrorsInErrorList)
            {
                _errorListProvider.Clear();
                return;
            }

            var errorCategory = _configuration.TaskErrorCategory;
            var cultures = _resourceManager.Cultures;
            var entries = _resourceManager.TableEntries;

            _errorListProvider.SetEntries(entries, cultures, (int)errorCategory);
        }

        private void Provider_Navigate(ResourceTableEntry entry)
        {
            ThrowIfNotOnUIThread();

            _shellViewModel.SelectEntry(entry);
        }

        public void Dispose()
        {
            _errorListProvider.Dispose();

            VS.Events.BuildEvents.SolutionBuildStarted -= BuildEvents_SolutionBuildStarted;
        }
    }
}
