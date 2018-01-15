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

    [Export]
    [VisualCompositionExport(RegionId.Shell)]
    public class ShellViewModel : ObservableObject
    {
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;

        [ImportingConstructor]
        public ShellViewModel([NotNull] ResourceViewModel resourceViewModel)
        {
            Contract.Requires(resourceViewModel != null);

            _resourceViewModel = resourceViewModel;

            resourceViewModel.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
        }

        public bool IsLoading { get; set; }

        public int SelectedTabIndex { get; set; }

        public void SelectEntry([NotNull] ResourceTableEntry entry)
        {
            SelectedTabIndex = 0;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                _resourceViewModel.SelectEntry(entry);
            });
        }

        private void SelectedEntities_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
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
    }
}
