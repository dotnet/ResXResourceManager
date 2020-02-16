namespace ResXManager.Model
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    public interface ISourceFilesProvider
    {
        [NotNull, ItemNotNull]
        IList<ProjectFile> SourceFiles { get; }

        void Invalidate();
    }
}
