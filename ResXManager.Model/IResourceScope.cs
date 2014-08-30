namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    [ContractClass(typeof (ResourceScopeContract))]
    public interface IResourceScope
    {
        IEnumerable<ResourceTableEntry> Entries
        {
            get;
        }

        IEnumerable<CultureInfo> Languages
        {
            get;
        }

        IEnumerable<CultureInfo> Comments
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
                throw new System.NotImplementedException();
            }
        }

        IEnumerable<CultureInfo> IResourceScope.Languages
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);
                throw new System.NotImplementedException();
            }
        }

        IEnumerable<CultureInfo> IResourceScope.Comments
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);
                throw new System.NotImplementedException();
            }
        }
    }
}
