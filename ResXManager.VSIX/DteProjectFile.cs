namespace tomenglertde.ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using EnvDTE;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;

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

        public override string Content
        {
            get
            {
                var projectItem = ProjectItem;

                try
                {
                    return projectItem.TryGetContent() ?? base.Content;
                }
                catch (IOException)
                {
                }

                projectItem.Open();
                return projectItem.TryGetContent() ?? string.Empty;
            }
            set
            {
                var projectItem = ProjectItem;

                try
                {
                    if (!projectItem.TrySetContent(value))
                    {
                        base.Content = value;
                    }
                }
                catch (IOException)
                {
                }

                projectItem.Open();
                projectItem.TrySetContent(value);
            }
        }

        public override bool IsWritable
        {
            get
            {
                return ProjectItem.Document.Maybe().Return(d => !d.ReadOnly, base.IsWritable);
            }
        }

        public bool HasChanges
        {
            get
            {
                return ProjectItem.Document.Maybe().Return(d => !d.Saved);
            }
        }

        private ProjectItem ProjectItem
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectItem>() != null);
                return ProjectItems.First();
            }
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