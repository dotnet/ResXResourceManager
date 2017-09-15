namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    public class ProjectFileEventArgs : EventArgs
    {
        public ProjectFileEventArgs([NotNull] ResourceLanguage language, [NotNull] ProjectFile projectFile)
        {
            Contract.Requires(language != null);
            Contract.Requires(projectFile != null);

            Language = language;
            ProjectFile = projectFile;
        }

        [NotNull]
        public ResourceLanguage Language { get; }

        [NotNull]
        public ProjectFile ProjectFile { get; }
    }
}