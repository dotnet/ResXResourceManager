namespace tomenglertde.ResXManager.View.Themes
{
    using System.Windows;

    using DataGridExtensions;

    public class ResourceLocator : IResourceLocator
    {
        public object FindResource(FrameworkElement target, object resourceKey)
        {
            var crk = resourceKey as ComponentResourceKey;

            var resourceId = crk?.ResourceId;

            // replace some of the resources with our own styled versions.

            return resourceId != null ? target?.TryFindResource(new ComponentResourceKey(typeof(ResourceKeys), resourceId)) : null;
        }
    }
}
