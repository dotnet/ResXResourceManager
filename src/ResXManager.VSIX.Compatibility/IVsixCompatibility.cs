namespace ResXManager.VSIX.Compatibility;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

using ResXManager.Model;

public interface IVsixCompatibility
{
    Task<ICollection<string>> GetSelectedFilesAsync();

    bool ContainsChildOfWinFormsDesignerItem(ResourceEntity entity, string? fileName);

    void RunCustomTool(IEnumerable<ResourceEntity> entities, string fileName);

    void RunCustomTool(ResourceEntity entity);

    Task<bool> AffectsResourceFileAsync(string? fileName);

    void SetFontSize(DependencyObject view);

    string EvaluateMoveToResourcePattern(
        string pattern,
        string text,
        string? key,
        bool reuseExisting,
        ResourceEntity? selectedResourceEntity,
        ResourceTableEntry? selectedResourceEntry);

    bool ActivateAlreadyOpenEditor(IEnumerable<ResourceLanguage> languages);

    void AddProjectItems(ResourceEntity entity, ResourceLanguage neutralLanguage, string languageFileName);
}
