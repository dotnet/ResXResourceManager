namespace ResXManager.VSIX.Compatibility
{
    using System.Collections.Generic;

    using ResXManager.Model;

    public interface IVsixShellViewModel
    {
        void SelectEntry(ResourceTableEntry entry);

        IMoveToResourceViewModel CreateMoveToResourceViewModel(ICollection<string> patterns,
            ICollection<ResourceEntity> resourceEntities, string text, string extension, string? className,
            string? functionName, string fileName);
    }
}
