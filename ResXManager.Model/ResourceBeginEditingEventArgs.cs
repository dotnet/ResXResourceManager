namespace tomenglertde.ResXManager.Model
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    /// <summary>
    /// Provides data for the <see cref="ResourceManager.BeginEditing"/> event.
    /// </summary>
    public class ResourceBeginEditingEventArgs : CancelEventArgs
    {
        [NotNull]
        private readonly ResourceEntity _entity;
        private readonly CultureKey _cultureKey;

        public ResourceBeginEditingEventArgs([NotNull] ResourceEntity entity, CultureKey cultureKey)
        {
            Contract.Requires(entity != null);

            _entity = entity;
            _cultureKey = cultureKey;
        }

        public CultureKey CultureKey => _cultureKey;

        [NotNull]
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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entity != null);
        }

    }
}
