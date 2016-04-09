namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class DteProjectFile : ProjectFile
    {
        private readonly DteSolution _solution;
        private readonly List<EnvDTE.ProjectItem> _projectItems = new List<EnvDTE.ProjectItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DteProjectFile" /> class.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="filePath">Name of the file.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="uniqueProjectName">Unique name of the project file.</param>
        /// <param name="projectItem">The project item, or null if the projectItem is not known.</param>
        public DteProjectFile(DteSolution solution, string filePath, string projectName, string uniqueProjectName, EnvDTE.ProjectItem projectItem)
            : base(filePath, solution.SolutionFolder, projectName, uniqueProjectName)
        {
            Contract.Requires(solution != null);
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            Contract.Requires(projectItem != null);

            _solution = solution;
            _projectItems.Add(projectItem);
        }

        /// <summary>
        /// Gets the project items.
        /// </summary>
        public IList<EnvDTE.ProjectItem> ProjectItems
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<EnvDTE.ProjectItem>>() != null);
                Contract.Ensures(Contract.Result<IList<EnvDTE.ProjectItem>>().Count > 0);

                return _projectItems;
            }
        }

        public void AddProject(string projectName, EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectName != null);
            Contract.Requires(projectItem != null);

            _projectItems.Add(projectItem);
            ProjectName += @", " + projectName;
        }

        public override XDocument Load()
        {
            var projectItem = DefaultProjectItem;

            try
            {
                return projectItem.TryGetContent() ?? base.Load();
            }
            catch (IOException)
            {
                // The file does not exist locally, but VS may download it when we call projectItem.Open()
            }

            projectItem.Open();
            return projectItem.TryGetContent() ?? new XDocument();
        }

        protected override void InternalSave(XDocument document)
        {
            var projectItem = DefaultProjectItem;

            try
            {
                if (!projectItem.TrySetContent(document))
                {
                    base.InternalSave(document);
                }
            }
            catch (IOException)
            {
                // The file does not exist locally, but VS may download it when we call projectItem.Open()
            }

            projectItem.Open();
            projectItem.TrySetContent(document);
        }

        public override bool IsWritable
        {
            get
            {
                return DefaultProjectItem.TryGetDocument().Maybe().Return(d => !d.ReadOnly, base.IsWritable);
            }
        }

        public bool HasChanges
        {
            get
            {
                return DefaultProjectItem.TryGetDocument().Maybe().Return(d => !d.Saved);
            }
        }

        public CodeGenerator CodeGenerator
        {
            get
            {
                return GetCodeGenerator();
            }
            set
            {
                this.SetCodeGenerator(value);
                OnPropertyChanged(() => CodeGenerator);
            }
        }

        public EnvDTE.ProjectItem DefaultProjectItem
        {
            get
            {
                Contract.Ensures(Contract.Result<EnvDTE.ProjectItem>() != null);
                return ProjectItems.First();
            }
        }

        public override bool IsWinFormsDesignerResource
        {
            get
            {
                var projectItem = DefaultProjectItem;
                var projectItems = projectItem.Collection;

                var parent = projectItems?.Parent as EnvDTE.ProjectItem;
                var subType = parent?.GetProperty("SubType") as string;

                return (subType == "Form") || (subType == "UserControl");
            }
        }

        public CodeGenerator GetCodeGenerator()
        {
            var projectItem = DefaultProjectItem;
            var containingProject = projectItem.ContainingProject;

            if ((containingProject == null) || (containingProject.Kind != ItemKind.CSharpProject))
                return CodeGenerator.None;

            var customTool = projectItem.GetCustomTool();

            if (string.IsNullOrEmpty(customTool))
            {
                if (IsWinFormsDesignerResource)
                    return CodeGenerator.WinForms;

                return projectItem.Children().Any(IsTextTemplate) ? CodeGenerator.TextTemplate : CodeGenerator.None;
            }

            CodeGenerator codeGenerator;
            return Enum.TryParse(customTool, out codeGenerator) ? codeGenerator : CodeGenerator.Unknown;
        }

        private static bool IsTextTemplate(EnvDTE.ProjectItem item)
        {
            Contract.Requires(item != null);

            var name = item.Name;

            return (name != null) && name.EndsWith(".tt", StringComparison.OrdinalIgnoreCase);
        }

        public void SetCodeGenerator(CodeGenerator value)
        {
            foreach (var projectItem in ProjectItems)
            {
                Contract.Assume(projectItem != null);
                var containingProject = projectItem.ContainingProject;

                if ((containingProject == null) || (containingProject.Kind != ItemKind.CSharpProject))
                    return;

                switch (value)
                {
                    case CodeGenerator.ResXFileCodeGenerator:
                    case CodeGenerator.PublicResXFileCodeGenerator:
                        SetCustomToolCodeGenerator(projectItem, value);
                        break;

                    case CodeGenerator.TextTemplate:
                        SetTextTemplateCodeGenerator(projectItem);
                        break;
                }
            }
        }

        private void SetTextTemplateCodeGenerator(EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            projectItem.SetCustomTool(null);

            const string t4FileName = "Resources.Designer.t4";

            if (!_solution.GetProjectFiles().Any(file => file.RelativeFilePath.Equals(t4FileName)))
            {
                var fullName = Path.Combine(_solution.SolutionFolder, t4FileName);
                File.WriteAllBytes(fullName, Resources.Resources_Designer_t4);
                _solution.AddFile(fullName);
            }

            var fileName = Path.ChangeExtension(FilePath, "Designer.tt");

            File.WriteAllBytes(fileName, Resources.Resources_Designer_tt);

            var item = projectItem.AddFromFile(fileName);
            if (item == null)
                return;

            item.SetProperty("BuildAction", 0);

            Dispatcher.BeginInvoke(() => item.RunCustomTool());
        }

        private static void SetCustomToolCodeGenerator(EnvDTE.ProjectItem projectItem, CodeGenerator value)
        {
            Contract.Requires(projectItem != null);

            projectItem.Children()
                .Where(IsTextTemplate)
                .ToArray()
                .ForEach(i => i.Delete());

            projectItem.SetCustomTool(value.ToString());
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