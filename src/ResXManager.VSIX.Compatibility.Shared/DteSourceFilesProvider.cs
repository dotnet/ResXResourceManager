namespace ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ResXManager.Model;
    using ResXManager.View.Tools;

    using TomsToolbox.Composition;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Shared, Export(typeof(ISourceFilesProvider))]
    internal sealed class DteSourceFilesProvider : ISourceFilesProvider
    {
        private readonly PerformanceTracer _performanceTracer;
        private readonly IConfiguration _configuration;
        private readonly DteSolution _solution;

        [ImportingConstructor]
        public DteSourceFilesProvider(IExportProvider exportProvider)
        {
            _performanceTracer = exportProvider.GetExportedValue<PerformanceTracer>();
            _configuration = exportProvider.GetExportedValue<IConfiguration>();
            _solution = exportProvider.GetExportedValue<DteSolution>();
        }

        public async Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken)
        {
            using (_performanceTracer.Start("Enumerate source files"))
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                return await Task.FromResult(_solution.GetProjectFiles(new FileFilter(_configuration)).ToList().AsReadOnly()).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the solution folder.
        /// </summary>
        public string SolutionFolder
        {
            get
            {
                ThrowIfNotOnUIThread();
                return _solution.SolutionFolder;
            }
        }
    }
}
