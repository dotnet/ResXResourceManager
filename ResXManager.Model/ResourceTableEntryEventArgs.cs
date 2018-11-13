namespace tomenglertde.ResXManager.Model
{
    using System;

    using JetBrains.Annotations;

    public sealed class ResourceTableEntryEventArgs : EventArgs
    {
        public ResourceTableEntryEventArgs([NotNull] ResourceTableEntry entry)
        {
            Entry = entry;
        }

        [NotNull]
        public ResourceTableEntry Entry { get; }
    }
}