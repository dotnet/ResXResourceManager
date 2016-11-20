namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Shell)]
    internal class ShellViewModel : ObservableObject
    {
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;
        [NotNull]
        private readonly DispatcherThrottle _updateThrottle;
        private bool _isLoading;

        [ImportingConstructor]
        public ShellViewModel([NotNull] ResourceViewModel resourceViewModel, [NotNull] PerformanceTracer performanceTracer)
        {
            Contract.Requires(resourceViewModel != null);
            Contract.Requires(performanceTracer != null);

            _performanceTracer = performanceTracer;
            _updateThrottle = new DispatcherThrottle(DispatcherPriority.Background, () => IsLoading = false);

            resourceViewModel.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_updateThrottle != null);
        }
    }
}
