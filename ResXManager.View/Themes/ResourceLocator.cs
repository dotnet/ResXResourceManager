namespace tomenglertde.ResXManager.View.Themes
{
    using System.Windows;

    using DataGridExtensions;

    public class ResourceLocator : IResourceLocator
    {
        public object FindResource(FrameworkElement target, object resourceKey)
        {
            var crk = resourceKey as ComponentResourceKey;

            // replace some of the resources with our own styled versions.
            return crk != null ? target?.TryFindResource(new ComponentResourceKey(typeof(ResourceKeys), crk.ResourceId)) : null;
        }
    }
}
