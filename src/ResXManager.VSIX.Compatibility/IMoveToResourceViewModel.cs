namespace ResXManager.VSIX.Compatibility
{
    using ResXManager.Model;

    public interface IMoveToResourceViewModel
    {
        string? Key { get; }

        bool ReuseExisiting { get; }

        ResourceEntity? SelectedResourceEntity { get; }

        string? Value { get; }
        string? Comment { get; }

        string? ReplacementValue { get; }
    }
}
