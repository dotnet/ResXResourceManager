namespace tomenglertde.ResXManager.Model
{
    using JetBrains.Annotations;

    public interface IFileFilter
    {
        bool IsSourceFile([NotNull] ProjectFile file);

        bool IncludeFile([NotNull] ProjectFile file);
    }
}