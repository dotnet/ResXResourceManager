namespace ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using ResXManager.Model;
    using ResXManager.View.Tools;

    using TomsToolbox.Composition;

    [Export(typeof(ISourceFilesProvider))]
    internal class DteSourceFilesProvider : ISourceFilesProvider
    {
        [NotNull]
        private readonly IExportProvider _exportProvider;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;
        [NotNull]
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public DteSourceFilesProvider([NotNull] IExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
            _performanceTracer = exportProvider.GetExportedValue<PerformanceTracer>();
            _configuration = exportProvider.GetExportedValue<Configuration>();
        }

        public async Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken)
        {
            using (_performanceTracer.Start("Enumerate source files"))
            {
                return await Task.FromResult(DteSourceFiles.ToList().AsReadOnly()).ConfigureAwait(false);
            }
        }

        public void Invalidate() => Solution.Invalidate();

        public string? SolutionFolder => Solution.SolutionFolder;

        [NotNull, ItemNotNull]
        private IEnumerable<ProjectFile> DteSourceFiles
        {
            get
            {
                var fileFilter = new FileFilter(_configuration);

                return GetProjectFiles(fileFilter);
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<ProjectFile> GetProjectFiles(IFileFilter fileFilter)
        {
            return Solution.GetProjectFiles(fileFilter);
        }

        [NotNull]
        private DteSolution Solution => _exportProvider.GetExportedValue<DteSolution>();
    }
}
