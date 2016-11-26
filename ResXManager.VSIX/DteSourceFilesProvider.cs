namespace tomenglertde.ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop.Composition;

    [Export(typeof(ISourceFilesProvider))]
    internal class DteSourceFilesProvider : ISourceFilesProvider
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;
        [NotNull]
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public DteSourceFilesProvider([NotNull] ICompositionHost compositionHost)
        {
            Contract.Requires(compositionHost != null);

            _compositionHost = compositionHost;
            _performanceTracer = compositionHost.GetExportedValue<PerformanceTracer>();
            _configuration = compositionHost.GetExportedValue<Configuration>();
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

        [NotNull, ItemNotNull]
        private IEnumerable<DteProjectFile> DteSourceFiles
        {
            get
            {
                var sourceFileFilter = new SourceFileFilter(_configuration);

                return GetProjectFiles().Where(p => p.IsResourceFile() || sourceFileFilter.IsSourceFile(p));
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<DteProjectFile> GetProjectFiles()
        {
            return _compositionHost.GetExportedValue<DteSolution>().GetProjectFiles();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
