namespace tomenglertde.ResXManager.View.Themes
{
    using System.ComponentModel.Composition;
    using System.Windows;

    using DataGridExtensions;

    [Export(typeof(IResourceLocator))]
    internal class ResourceLocator : IResourceLocator
    {
        public object FindResource(FrameworkElement target, object resourceKey)
        {
            var crk = resourceKey as ComponentResourceKey;

            // must redirect all resources!
            // WPF is not able to find component resource in DGX if multiple extensions have multiple versions of the same assembly 
            // loaded in the VS process.
            return crk != null ? target?.FindResource(new ComponentResourceKey(typeof(ResourceKeys), crk.ResourceId)) : null;
        }
    }
}
