namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using TomsToolbox.Desktop;

    /// <summary>
    /// Represents a file associated with a project.
    /// </summary>
    public class ProjectFile : ObservableObject
    {
        private readonly string _filePath;
        private readonly string _extension;
        private string _fingerPrint;
        private XDocument _document;
        private readonly string _relativeFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFile" /> class.
        /// </summary>
        /// <param name="filePath">Name of the file.</param>
        /// <param name="rootFolder">The root folder to calculate the relative path from.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="uniqueProjectName">Unique name of the project file.</param>
        public ProjectFile(string filePath, string rootFolder, string projectName, string uniqueProjectName)
        {
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            Contract.Requires(rootFolder != null);

            _filePath = filePath;
            _relativeFilePath = GetRelativePath(rootFolder, filePath);
            _extension = Path.GetExtension(_filePath);

            ProjectName = projectName;
            UniqueProjectName = uniqueProjectName;
        }

        /// <summary>
        /// Gets the file name of the file.
        /// </summary>
        public string FilePath
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

                return _filePath;
            }
        }

        public string Extension
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _extension;
            }
        }

        /// <summary>
        /// Gets or sets the name of the project containing the file.
        /// </summary>
        public string ProjectName
        {
            get;
            set;
        }

        public string UniqueProjectName
        {
            get;
            set;
        }

        public string RelativeFilePath
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _relativeFilePath;
            }
        }

        public bool HasChanges { get; protected set; }

        public XDocument Load()
        {
            Contract.Ensures(Contract.Result<XDocument>() != null);

            var document = InternalLoad();

            _document = document;
            _fingerPrint = document.ToString(SaveOptions.DisableFormatting);

            HasChanges = false;

            return document;
        }

        protected virtual XDocument InternalLoad()
        {
            Contract.Ensures(Contract.Result<XDocument>() != null);

            return XDocument.Load(FilePath);
        }

        public void Changed()
        {
            if (_document == null)
                return;

            InternalChanged(_document);
        }

        protected virtual void InternalChanged(XDocument document)
        {
            Contract.Requires(document != null);

            HasChanges = _fingerPrint != document.ToString(SaveOptions.DisableFormatting);
        }

        public void Save()
        {
            var document = _document;

            if (document == null)
                return;

            InternalSave(document);

            HasChanges = false;

            _fingerPrint = document.ToString(SaveOptions.DisableFormatting);
        }

        protected virtual void InternalSave(XDocument document)
        {
            Contract.Requires(document != null);

            document.Save(FilePath);
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
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                return false;
            }
        }

        public virtual bool IsWinFormsDesignerResource => false;

        private static string GetRelativePath(string solutionFolder, string filePath)
        {
            Contract.Requires(solutionFolder != null);
            Contract.Requires(filePath != null);
            Contract.Ensures(Contract.Result<string>() != null);

            solutionFolder = solutionFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            filePath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (!solutionFolder.Any() || (solutionFolder.Last() != Path.DirectorySeparatorChar))
            {
                solutionFolder += Path.DirectorySeparatorChar;
            }

            return filePath.StartsWith(solutionFolder, StringComparison.OrdinalIgnoreCase) ? filePath.Substring(solutionFolder.Length) : filePath;

        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!string.IsNullOrEmpty(_filePath));
            Contract.Invariant(_extension != null);
            Contract.Invariant(_relativeFilePath != null);
        }
    }
}
