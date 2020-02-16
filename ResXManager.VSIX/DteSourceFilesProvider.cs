namespace ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

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

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                using (_performanceTracer.Start("Enumerate source files"))
                {
                    return DteSourceFiles.Cast<ProjectFile>().ToArray();
                }
            }
        }

        public void Invalidate() => Solution.Invalidate();

        [NotNull, ItemNotNull]
        private IEnumerable<DteProjectFile> DteSourceFiles
        {
            get
            {
                var fileFilter = new FileFilter(_configuration);

                return GetProjectFiles().Where(p => fileFilter.IncludeFile(p) && (p.IsResourceFile() || fileFilter.IsSourceFile(p)));
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<DteProjectFile> GetProjectFiles()
        {
            return Solution.GetProjectFiles();
        }

        [NotNull]
        private DteSolution Solution => _exportProvider.GetExportedValue<DteSolution>();
    }
}
