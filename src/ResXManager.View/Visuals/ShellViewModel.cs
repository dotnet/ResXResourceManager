namespace ResXManager.View.Visuals
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Composition;
    using System.Windows.Threading;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using Throttle;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [Export, Shared]
    [VisualCompositionExport(RegionId.Shell)]
    public partial class ShellViewModel : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

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

            _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
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
                _dispatcher.ProcessMessages(DispatcherPriority.Render);
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
