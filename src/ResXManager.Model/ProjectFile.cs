namespace ResXManager.Model;

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

/// <summary>
/// Represents a file associated with a project.
/// </summary>
public class ProjectFile : XmlFile, INotifyPropertyChanged
{
    private string? _fingerPrint;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectFile" /> class.
    /// </summary>
    /// <param name="filePath">Name of the file.</param>
    /// <param name="rootFolder">The root folder to calculate the relative path from.</param>
    /// <param name="projectName">Name of the project.</param>
    /// <param name="uniqueProjectName">Unique name of the project file.</param>
    public ProjectFile(string filePath, string rootFolder, string? projectName, string? uniqueProjectName)
        : base(filePath)
    {
        RelativeFilePath = GetRelativePath(rootFolder, filePath);
        Extension = Path.GetExtension(FilePath);

        ProjectName = projectName;
        UniqueProjectName = uniqueProjectName;
    }

    public string Extension { get; }

    /// <summary>
    /// Gets or sets the name of the project containing the file.
    /// </summary>
    public string? ProjectName { get; set; }

    public string? UniqueProjectName { get; set; }

    public string RelativeFilePath { get; }

    public bool HasChanges { get; private set; }

    public XDocument Load()
    {
        var document = LoadFromFile();

        _fingerPrint = document.ToString(SaveOptions.DisableFormatting);

        HasChanges = false;

        return document;
    }

    public bool Changed(XDocument document)
    {
        HasChanges = _fingerPrint != document.ToString(SaveOptions.DisableFormatting);

        return HasChanges;
    }

    public void Save(XDocument? document)
    {
        if (document == null)
            return;

        SaveToFile(document);

        HasChanges = false;

        _fingerPrint = document.ToString(SaveOptions.DisableFormatting);
    }

    public virtual bool IsWinFormsDesignerResource => false;

    private static string GetRelativePath(string solutionFolder, string filePath)
    {
        solutionFolder = solutionFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        filePath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (!solutionFolder.Any() || (solutionFolder.Last() != Path.DirectorySeparatorChar))
        {
            solutionFolder += Path.DirectorySeparatorChar;
        }

        return filePath.StartsWith(solutionFolder, StringComparison.OrdinalIgnoreCase) ? filePath.Substring(solutionFolder.Length) : filePath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
