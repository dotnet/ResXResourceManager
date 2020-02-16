namespace ResXManager.View.Themes
{
    using System.Windows;

    using DataGridExtensions;

    using JetBrains.Annotations;

    public class ResourceLocator : IResourceLocator
    {
        [CanBeNull]
        public object FindResource([CanBeNull] FrameworkElement target, [CanBeNull] object resourceKey)
        {
            var crk = resourceKey as ComponentResourceKey;

            var resourceId = crk?.ResourceId;

            // replace some of the resources with our own styled versions.

            return resourceId != null ? target?.TryFindResource(new ComponentResourceKey(typeof(ResourceKeys), resourceId)) : null;
        }
    }
}
