namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Represents a file associated with a project.
    /// </summary>
    public class ProjectFile
    {
        private readonly string _filePath;
        private readonly string _extension;

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
            Contract.Requires(!String.IsNullOrEmpty(rootFolder));

            _filePath = filePath;
            RelativeFilePath = GetRelativePath(rootFolder, filePath);
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
            private set;
        }

        public string RelativeFilePath
        {
            get; 
            private set;
        }

        private static string GetRelativePath(string solutionFolder, string filePath)
        {
            Contract.Requires(!String.IsNullOrEmpty(solutionFolder));
            Contract.Requires(filePath != null);

            solutionFolder = solutionFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            filePath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if ((solutionFolder.Count() == 0) || (solutionFolder.Last() != Path.DirectorySeparatorChar))
            {
                solutionFolder += Path.DirectorySeparatorChar;
            }

            return filePath.StartsWith(solutionFolder, StringComparison.OrdinalIgnoreCase) ? filePath.Substring(solutionFolder.Length) : filePath;

        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!String.IsNullOrEmpty(_filePath));
            Contract.Invariant(_extension != null);
            Contract.Invariant(RelativeFilePath != null);
        }
    }
}
