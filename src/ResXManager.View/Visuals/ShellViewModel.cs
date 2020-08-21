namespace ResXManager.View.Visuals
{
    using System.Collections.Specialized;
    using System.Composition;
    using System.Windows.Threading;

    using Throttle;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [Export, Shared]
    [VisualCompositionExport(RegionId.Shell)]
    public class ShellViewModel : ObservableObject
    {
        [ImportingConstructor]
        public ShellViewModel(ResourceViewModel resourceViewModel)
        {
            ResourceViewModel = resourceViewModel;

            resourceViewModel.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
        }

        public ResourceViewModel ResourceViewModel { get; }

        public bool IsLoading { get; set; }

        public int SelectedTabIndex { get; set; }

        public void SelectEntry(ResourceTableEntry entry)
        {
            SelectedTabIndex = 0;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                ResourceViewModel.SelectEntry(entry);
            });
        }

        private void SelectedEntities_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
