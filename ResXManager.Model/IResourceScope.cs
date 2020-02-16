namespace ResXManager.Model
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    public interface IResourceScope
    {
        [NotNull]
        [ItemNotNull]
        IEnumerable<ResourceTableEntry> Entries
        {
            get;
        }

        [NotNull]
        [ItemNotNull]
        IEnumerable<CultureKey> Languages
        {
            get;
        }

        [NotNull]
        [ItemNotNull]
        IEnumerable<CultureKey> Comments
        {
            get;
        }
    }
}
