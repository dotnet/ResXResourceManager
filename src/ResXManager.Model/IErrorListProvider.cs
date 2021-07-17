namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;

    using ResXManager.Infrastructure;

    public interface IErrorListProvider : IDisposable
    {
        event Action<ResourceTableEntry> Navigate;

        void SetEntries(ICollection<ResourceTableEntry> entries, ICollection<CultureKey> cultures, int errorCategory);
        void Remove(ResourceTableEntry entry);
        void Clear();
    }
}