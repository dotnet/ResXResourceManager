namespace ResXManager.View.Tools;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ResXManager.Model;

public interface ISourceFilesProvider
{
    Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken);

    string? SolutionFolder { get; }
}