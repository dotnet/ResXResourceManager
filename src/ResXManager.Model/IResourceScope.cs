namespace ResXManager.Model
{
    using System.Collections.Generic;

    using ResXManager.Infrastructure;

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
}
