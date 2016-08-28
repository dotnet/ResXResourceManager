namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Runs a project items custom tool only once, even if it gets triggered multiple times during change.
    /// </summary>
    internal sealed class CustomToolRunner : IDisposable
    {
        private HashSet<EnvDTE.ProjectItem> _projectItems = new HashSet<EnvDTE.ProjectItem>();
        private readonly DispatcherThrottle _throttle;

        public CustomToolRunner()
        {
            _throttle = new DispatcherThrottle(RunCustomTool);
        }

        public void Enqueue(IEnumerable<EnvDTE.ProjectItem> projectItems)
        {
            if (projectItems == null)
                return;

            _projectItems.AddRange(projectItems);

            _throttle.Tick();
        }

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
        private void ObjectInvariant()
        {
            Contract.Invariant(_projectItems != null);
            Contract.Invariant(_throttle != null);
        }
    }
}
