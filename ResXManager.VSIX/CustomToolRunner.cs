namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Runs a project items custom tool only once, even if it gets triggered multiple times during change.
    /// </summary>
    internal sealed class CustomToolRunner : IDisposable
    {
        [NotNull]
        [ItemNotNull]
        private HashSet<EnvDTE.ProjectItem> _projectItems = new HashSet<EnvDTE.ProjectItem>();

        public void Enqueue([ItemNotNull] IEnumerable<EnvDTE.ProjectItem> projectItems)
        {
            if (projectItems == null)
                return;

            _projectItems.AddRange(projectItems);

            RunCustomTool();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void RunCustomTool()
        {
            _projectItems.ForEach(projectItem => projectItem.RunCustomTool());
            _projectItems = new HashSet<EnvDTE.ProjectItem>();
        }

        public void Dispose()
        {
            RunCustomTool();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_projectItems != null);
        }
    }
}
