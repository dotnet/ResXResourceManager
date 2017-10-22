namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    public class FileFilter : IFileFilter
    {
        [NotNull]
        [ItemNotNull]
        private readonly string[] _extensions;
        [CanBeNull]
        private readonly Regex _fileExclusionFilter;

        public FileFilter([NotNull] Configuration configuration)
        {
            Contract.Requires(configuration != null);

            _extensions = configuration.CodeReferences
                .Items.SelectMany(item => item.ParseExtensions())
                .Distinct()
                .ToArray();

            _fileExclusionFilter = configuration.FileExclusionFilter.TryCreateRegex();
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return _extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
        }

        public bool IncludeFile(FileInfo fileInfo)
        {
            return _fileExclusionFilter?.IsMatch(fileInfo.FullName) != true;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_extensions != null);
        }
    }
}