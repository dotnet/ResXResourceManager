namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;

    [ContractClass(typeof (ResourceScopeContract))]
    public interface IResourceScope
    {
        IEnumerable<ResourceTableEntry> Entries
        {
            get;
        }

        IEnumerable<CultureKey> Languages
        {
            get;
        }

        IEnumerable<CultureKey> Comments
        {
            get;
        }
    }

    [ContractClassFor(typeof (IResourceScope))]
    abstract class ResourceScopeContract : IResourceScope
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
