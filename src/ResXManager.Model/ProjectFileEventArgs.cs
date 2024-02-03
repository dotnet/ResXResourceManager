namespace ResXManager.Model;

using System;

public class ProjectFileEventArgs : EventArgs
{
    public ProjectFileEventArgs(ResourceLanguage language, ProjectFile projectFile)
    {
        Language = language;
        ProjectFile = projectFile;
    }

    public ResourceLanguage Language { get; }

    public ProjectFile ProjectFile { get; }
}