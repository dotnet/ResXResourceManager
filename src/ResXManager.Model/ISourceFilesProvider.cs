namespace ResXManager.Model;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ISourceFilesProvider
{
    Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken);

    string? SolutionFolder { get; }
}
