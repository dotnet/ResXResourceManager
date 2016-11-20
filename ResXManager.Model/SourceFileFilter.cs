namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    public class SourceFileFilter : ISourceFileFilter
    {
        [NotNull]
        private readonly string[] _extensions;

        public SourceFileFilter([NotNull] Configuration configuration)
        {
            Contract.Requires(configuration != null);

            _extensions = configuration.CodeReferences
                .Items.SelectMany(item => item.ParseExtensions())
                .Distinct()
                .ToArray();
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return _extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
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