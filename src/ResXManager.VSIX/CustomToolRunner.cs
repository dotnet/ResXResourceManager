namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    /// <summary>
    /// Runs a project items custom tool only once, even if it gets triggered multiple times during change.
    /// </summary>
    internal sealed class CustomToolRunner : IDisposable
    {
        private HashSet<EnvDTE.ProjectItem> _projectItems = new();

        public void Enqueue(IEnumerable<EnvDTE.ProjectItem>? projectItems)
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
    }
}
