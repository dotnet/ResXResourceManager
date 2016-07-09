namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Shell)]
    class ShellViewModel : ObservableObject
    {
        private readonly DispatcherThrottle _updateThrottle;
        private bool _isLoading;

        [ImportingConstructor]
        public ShellViewModel(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);

            _updateThrottle = new DispatcherThrottle(DispatcherPriority.Background, () => IsLoading = false);

            resourceManager.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                SetProperty(ref _isLoading, value, nameof(IsLoading));
            }
        }

        private void SelectedEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _updateThrottle.Tick();

            IsLoading = true;

            Dispatcher.ProcessMessages(DispatcherPriority.Render);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_updateThrottle != null);
        }
    }
}
