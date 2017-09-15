namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    public class ProjectFileEventArgs : EventArgs
    {
        public ProjectFileEventArgs([NotNull] ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            ProjectFile = projectFile;
        }

        [NotNull]
        public ProjectFile ProjectFile { get; }
    }
}