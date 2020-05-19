namespace ResXManager.View.Visuals
{
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Throttle;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.Mef;

    [Export]
    [VisualCompositionExport(RegionId.Shell)]
    public class ShellViewModel : ObservableObject
    {
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;

        [ImportingConstructor]
        public ShellViewModel([NotNull] ResourceViewModel resourceViewModel)
        {
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

            try
            {
                Dispatcher.ProcessMessages(DispatcherPriority.Render);
            }
            catch
            {
                // sometimes dispatcher processing is suspended, just ignore, as this is only used to improve UI responsiveness
            }
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Background)]
        private void Update()
        {
            IsLoading = false;
        }
    }
}
