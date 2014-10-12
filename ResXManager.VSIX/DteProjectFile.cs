namespace tomenglertde.ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using EnvDTE;
    using tomenglertde.ResXManager.Model;

    internal class DteProjectFile : ProjectFile
    {
        private readonly List<ProjectItem> _projectItems = new List<ProjectItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DteProjectFile" /> class.
        /// </summary>
        /// <param name="filePath">Name of the file.</param>
        /// <param name="rootFolder"></param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="uniqueProjectName">Unique name of the project file.</param>
        /// <param name="projectItem">The project item, or null if the projectItem is not known.</param>
        public DteProjectFile(string filePath, string rootFolder, string projectName, string uniqueProjectName, ProjectItem projectItem)
            : base(filePath, rootFolder, projectName, uniqueProjectName)
        {
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            Contract.Requires(rootFolder != null);
            Contract.Requires(projectItem != null);

            _projectItems.Add(projectItem);
        }

        /// <summary>
        /// Gets the project items.
        /// </summary>
        public IList<ProjectItem> ProjectItems
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ProjectItem>>() != null);
                Contract.Ensures(Contract.Result<IList<ProjectItem>>().Count > 0);

                return _projectItems;
            }
        }

        public void AddProject(string projectName, ProjectItem projectItem)
        {
            Contract.Requires(projectName != null);
            Contract.Requires(projectItem != null);

            _projectItems.Add(projectItem);
            ProjectName += @", " + projectName;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_projectItems != null);
            Contract.Invariant(_projectItems.Count > 0);
        }
    }
}