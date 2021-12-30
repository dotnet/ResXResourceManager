namespace ResXManager.VSIX.Compatibility
{
    using ResXManager.Model;

    public interface IMoveToResourceViewModel
    {
        string? Key { get; }

        bool ReuseExisting { get; }

        ResourceEntity? SelectedResourceEntity { get; }

        ResourceTableEntry? SelectedResourceEntry { get; set; }

        string? Value { get; }

        string? Comment { get; }

        string? ReplacementValue { get; }
    }
}
