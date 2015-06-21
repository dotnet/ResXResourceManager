namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport("Content", Sequence = 1)]
    class ResourceViewModel : ObservableObject, IComposablePart
    {
        private readonly ResourceManager _resourceManager;

        [ImportingConstructor]
        public ResourceViewModel(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);
            _resourceManager = resourceManager;
        }

        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
            }
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Main;
        }
    }
}
