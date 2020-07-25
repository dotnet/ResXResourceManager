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

    [Export(typeof(ISourceFilesProvider))]
    internal class DteSourceFilesProvider : ISourceFilesProvider
    {
        private readonly IExportProvider _exportProvider;
        private readonly PerformanceTracer _performanceTracer;
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public DteSourceFilesProvider(IExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
            _performanceTracer = exportProvider.GetExportedValue<PerformanceTracer>();
            _configuration = exportProvider.GetExportedValue<Configuration>();
        }

        public async Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken)
        {
            using (_performanceTracer.Start("Enumerate source files"))
            {
#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
                return await Task.FromResult(DteSourceFiles.ToList().AsReadOnly()).ConfigureAwait(false);
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.
            }
        }

        public void Invalidate() => Solution.Invalidate();

        /// <summary>
        /// Gets the solution folder.
        /// </summary>
        /// <value>
        /// The solution folder.
        /// </value>
#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
        public string? SolutionFolder => Solution.SolutionFolder;
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

        private IEnumerable<ProjectFile> DteSourceFiles
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                var fileFilter = new FileFilter(_configuration);

                return GetProjectFiles(fileFilter);
            }
        }

        private IEnumerable<ProjectFile> GetProjectFiles(IFileFilter fileFilter)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return Solution.GetProjectFiles(fileFilter);
        }

        private DteSolution Solution => _exportProvider.GetExportedValue<DteSolution>();
    }
}
