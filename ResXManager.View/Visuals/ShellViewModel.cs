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

    using Throttle;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Shell)]
    internal class ShellViewModel : ObservableObject
    {
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        [ImportingConstructor]
        public ShellViewModel([NotNull] ResourceViewModel resourceViewModel, [NotNull] PerformanceTracer performanceTracer)
        {
            Contract.Requires(resourceViewModel != null);
            Contract.Requires(performanceTracer != null);

            _performanceTracer = performanceTracer;

            resourceViewModel.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
        }

        public bool IsLoading { get; set; }

        private void SelectedEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Update();

            IsLoading = true;

            Dispatcher.ProcessMessages(DispatcherPriority.Render);
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Background)]
        private void Update()
        {
            IsLoading = false;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_performanceTracer != null);
        }
    }
}
