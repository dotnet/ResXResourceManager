namespace tomenglertde.ResXManager.Model
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    /// <summary>
    /// Provides data for the <see cref="ResourceManager.BeginEditing"/> event.
    /// </summary>
    public class ResourceBeginEditingEventArgs : CancelEventArgs
    {
        private readonly ResourceEntity _entity;
        private readonly CultureInfo _language;

        public ResourceBeginEditingEventArgs(ResourceEntity entity, CultureInfo language)
        {
            Contract.Requires(entity != null);
            _entity = entity;
            _language = language;
        }

        public CultureInfo Language
        {
            get
            {
                return _language;
            }
        }

        public ResourceEntity Entity
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _entity;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entity != null);
        }

    }
}
