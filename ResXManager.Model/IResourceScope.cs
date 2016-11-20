namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    [ContractClass(typeof (ResourceScopeContract))]
    public interface IResourceScope
    {
        [NotNull]
        IEnumerable<ResourceTableEntry> Entries
        {
            get;
        }

        [NotNull]
        IEnumerable<CultureKey> Languages
        {
            get;
        }

        [NotNull]
        IEnumerable<CultureKey> Comments
        {
            get;
        }
    }

    [ContractClassFor(typeof (IResourceScope))]
    internal abstract class ResourceScopeContract : IResourceScope
    {
        IEnumerable<ResourceTableEntry> IResourceScope.Entries
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ResourceTableEntry>>() != null);
                throw new NotImplementedException();
            }
        }

        IEnumerable<CultureKey> IResourceScope.Languages
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureKey>>() != null);
                throw new NotImplementedException();
            }
        }

        IEnumerable<CultureKey> IResourceScope.Comments
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureKey>>() != null);
                throw new NotImplementedException();
            }
        }
    }
}
