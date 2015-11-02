namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public class SourceFileFilter
    {
        private readonly string[] _extensions;

        public SourceFileFilter(Configuration configuration)
        {
            Contract.Requires(configuration != null);

            _extensions = configuration.CodeReferences
                .Items.SelectMany(item => item.ParseExtensions())
                .Distinct()
                .ToArray();
        }

        public bool IsSourceFile(ProjectFile file)
        {
            Contract.Requires(file != null);

            return _extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_extensions != null);
        }
    }
}