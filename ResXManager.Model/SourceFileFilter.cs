namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text.RegularExpressions;

    internal class SourceFileFilter
    {
        private readonly string _includeSourceFilePattern = (ResourceManagerExtensions.Settings.DetectCodeReferences_Include ?? String.Empty).Trim();
        private readonly string _excludeSourceFilePattern = (ResourceManagerExtensions.Settings.DetectCodeReferences_Exclude ?? String.Empty).Trim();
        private readonly Regex _includeSourceFileExpression;
        private readonly Regex _excludeSourceFileExpression;

        public SourceFileFilter()
        {
            _includeSourceFileExpression = new Regex(_includeSourceFilePattern);
            _excludeSourceFileExpression = new Regex(_excludeSourceFilePattern);
        }

        public bool IsSourceFile(string fileName)
        {
            var isSourceFile = (String.IsNullOrEmpty(_includeSourceFilePattern) || _includeSourceFileExpression.Match(fileName).Success)
                               && (String.IsNullOrEmpty(_excludeSourceFilePattern) || !_excludeSourceFileExpression.Match(fileName).Success);

            return isSourceFile;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_includeSourceFilePattern != null);
            Contract.Invariant(_includeSourceFileExpression != null);
            Contract.Invariant(_excludeSourceFilePattern != null);
            Contract.Invariant(_excludeSourceFileExpression != null);
        }
    }
}