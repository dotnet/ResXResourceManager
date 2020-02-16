namespace ResXManager.Model
{
    using System;

    using JetBrains.Annotations;

    public class ProjectFileEventArgs : EventArgs
    {
        public ProjectFileEventArgs([NotNull] ResourceLanguage language, [NotNull] ProjectFile projectFile)
        {
            Language = language;
            ProjectFile = projectFile;
        }

        [NotNull]
        public ResourceLanguage Language { get; }

        [NotNull]
        public ProjectFile ProjectFile { get; }
    }
}