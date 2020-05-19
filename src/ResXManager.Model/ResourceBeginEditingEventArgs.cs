namespace ResXManager.Model
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    /// <summary>
    /// Provides data for the <see cref="ResourceManager.BeginEditing"/> event.
    /// </summary>
    public class ResourceBeginEditingEventArgs : CancelEventArgs
    {
        public ResourceBeginEditingEventArgs([NotNull] ResourceEntity entity, [CanBeNull] CultureKey cultureKey)
        {
            Entity = entity;
            CultureKey = cultureKey;
        }

        [CanBeNull]
        public CultureKey CultureKey { get; }

        [NotNull]
        public ResourceEntity Entity { get; }
    }
}
