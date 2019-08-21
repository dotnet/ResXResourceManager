namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf;

    /// <summary>
    /// Represents a file associated with a project.
    /// </summary>
    public class ProjectFile : ObservableObject
    {
        [CanBeNull] private string _fingerPrint;

        private readonly XmlWriterSettings _xmlWriterSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFile" /> class.
        /// </summary>
        /// <param name="filePath">Name of the file.</param>
        /// <param name="rootFolder">The root folder to calculate the relative path from.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="uniqueProjectName">Unique name of the project file.</param>
        public ProjectFile([NotNull] string filePath, [NotNull] string rootFolder, [CanBeNull] string projectName, [CanBeNull] string uniqueProjectName)
        {
            FilePath = filePath;
            RelativeFilePath = GetRelativePath(rootFolder, filePath);
            Extension = Path.GetExtension(FilePath);

            ProjectName = projectName;
            UniqueProjectName = uniqueProjectName;
            _xmlWriterSettings = new XmlWriterSettings
            {
                // The false means, do not emit the BOM.
                Encoding = new UTF8Encoding(false),
                Indent = true
            };
        }

        /// <summary>
        /// Gets the file name of the file.
        /// </summary>
        [NotNull]
        public string FilePath { get; }

        [NotNull]
        public string Extension { get; }

        /// <summary>
        /// Gets or sets the name of the project containing the file.
        /// </summary>
        [CanBeNull]
        public string ProjectName { get; set; }

        [CanBeNull]
        public string UniqueProjectName { get; set; }

        [NotNull]
        public string RelativeFilePath { get; }

        public bool HasChanges { get; protected set; }

        [NotNull]
        public XDocument Load()
        {
            var document = InternalLoad();

            _fingerPrint = document.ToString(SaveOptions.DisableFormatting);

            HasChanges = false;

            return document;
        }

        [NotNull]
        protected virtual XDocument InternalLoad()
        {
            return XDocument.Load(FilePath);
        }

        public void Changed([CanBeNull] XDocument document, bool willSaveImmediately)
        {
            if (document == null)
                return;

            InternalChanged(document, willSaveImmediately);
        }

        protected virtual void InternalChanged([NotNull] XDocument document, bool willSaveImmediately)
        {
            HasChanges = _fingerPrint != document.ToString(SaveOptions.DisableFormatting);
        }

        public void Save([CanBeNull] XDocument document)
        {
            if (document == null)
                return;

            InternalSave(document);

            HasChanges = false;

            _fingerPrint = document.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual void InternalSave([NotNull] XDocument document)
        {
            using (XmlWriter w = XmlWriter.Create(FilePath, _xmlWriterSettings))
            {
                document.Save(w);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the file associated with this instance can be written.
        /// </summary>
        public virtual bool IsWritable
        {
            get
            {
                try
                {
                    if ((File.GetAttributes(FilePath) & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                        return false;

                    using (File.Open(FilePath, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                return false;
            }
        }

        public virtual bool IsWinFormsDesignerResource => false;

        [NotNull]
        private static string GetRelativePath([NotNull] string solutionFolder, [NotNull] string filePath)
        {
            solutionFolder = solutionFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            filePath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (!solutionFolder.Any() || (solutionFolder.Last() != Path.DirectorySeparatorChar))
            {
                solutionFolder += Path.DirectorySeparatorChar;
            }

            return filePath.StartsWith(solutionFolder, StringComparison.OrdinalIgnoreCase) ? filePath.Substring(solutionFolder.Length) : filePath;
        }
    }
}
