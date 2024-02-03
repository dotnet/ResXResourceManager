namespace ResXManager.Model;

using System;

public sealed class ResourceTableEntryEventArgs : EventArgs
{
    public ResourceTableEntryEventArgs(ResourceTableEntry entry)
    {
        Entry = entry;
    }

    public ResourceTableEntry Entry { get; }
}