namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    public interface ISourceFilesProvider
    {
        [NotNull]
        [ItemNotNull]
        Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken);

        string? SolutionFolder { get; }

        void Invalidate();
    }
}
