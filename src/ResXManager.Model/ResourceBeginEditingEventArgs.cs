namespace ResXManager.Model
{
    using System.ComponentModel;

    using ResXManager.Infrastructure;

    /// <summary>
    /// Provides data for the <see cref="ResourceManager.BeginEditing"/> event.
    /// </summary>
    public class ResourceBeginEditingEventArgs : CancelEventArgs
    {
        public ResourceBeginEditingEventArgs(ResourceEntity entity, CultureKey? cultureKey)
        {
            Entity = entity;
            CultureKey = cultureKey;
        }

        public CultureKey? CultureKey { get; }

        public ResourceEntity Entity { get; }
    }
}
