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
    internal class ShellViewModel : ObservableObject
    {
        private readonly PerformanceTracer _performanceTracer;
        private readonly DispatcherThrottle _updateThrottle;
        private bool _isLoading;

        [ImportingConstructor]
        public ShellViewModel(ResourceManager resourceManager, PerformanceTracer performanceTracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(performanceTracer != null);

            _performanceTracer = performanceTracer;
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

            // using (_performanceTracer.Start("Dispatcher.ProcessMessages"))
            {
                Dispatcher.ProcessMessages(DispatcherPriority.Render);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_updateThrottle != null);
        }
    }
}
