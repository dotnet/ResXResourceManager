namespace ResXManager.Model;

public interface IFileFilter
{
    bool IsSourceFile(ProjectFile file);

    bool IncludeFile(ProjectFile file);
}