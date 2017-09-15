namespace ResXManager.Scripting
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop.Composition;

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    internal class SourceFilesProvider : ISourceFilesProvider, IFileFilter
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost;

        private Regex _fileExclusionFilter;

        [ImportingConstructor]
        public SourceFilesProvider([NotNull] ICompositionHost compositionHost)
        {
            Contract.Requires(compositionHost != null);

            _compositionHost = compositionHost;
        }

        public string Folder { get; set; }

        public string ExclusionFilter { get; set; }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = Folder;
                if (string.IsNullOrEmpty(folder))
                    return new ProjectFile[0];

                _fileExclusionFilter = ExclusionFilter.TryCreateRegex();

                return new DirectoryInfo(folder).GetAllSourceFiles(this);
            }
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return false;
        }

        public bool IncludeFile(FileInfo fileInfo)
        {
            return _fileExclusionFilter?.IsMatch(fileInfo.FullName) != true;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
        }
    }
}