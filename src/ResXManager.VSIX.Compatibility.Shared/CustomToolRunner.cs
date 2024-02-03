namespace ResXManager.VSIX;

using System;
using System.Collections.Generic;
using System.Composition;

using Throttle;

using TomsToolbox.Essentials;
using TomsToolbox.Wpf;

using static Microsoft.VisualStudio.Shell.ThreadHelper;

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.

/// <summary>
/// Runs a project items custom tool only once, even if it gets triggered multiple times during change.
/// </summary>
[Shared, Export(typeof(ICustomToolRunner))]
internal sealed class CustomToolRunner : IDisposable, ICustomToolRunner
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
        ThrowIfNotOnUIThread();

        _projectItems.ForEach(projectItem => projectItem.RunCustomTool());
        _projectItems = new HashSet<EnvDTE.ProjectItem>();
    }

    public void Dispose()
    {
        RunCustomTool();
    }
}