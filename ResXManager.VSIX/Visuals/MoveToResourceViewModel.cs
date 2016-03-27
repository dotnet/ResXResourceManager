namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using EnvDTE;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class MoveToResourceViewModel : ObservableObject
    {
        private readonly ICollection<string> _patterns;
        private readonly ResourceManager _resourceManager;
        private readonly ObservableCollection<string> _replacements = new ObservableCollection<string>();
        private ResourceEntity _selectedResourceEntity;
        private string _key;
        private string _value;
        private string _comment;
        private string _replacement;
        private string _namespace;

        public MoveToResourceViewModel(ExportProvider exportProvider, ICollection<string> patterns)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(patterns != null);

            _patterns = patterns;

            _resourceManager = exportProvider.GetExportedValue<ResourceManager>();
            _resourceManager.Reload();
        }

        public ICollection<ResourceEntity> ResourceEntities => _resourceManager.ResourceEntities;

        [Required]
        public ResourceEntity SelectedResourceEntity
        {
            get
            {
                return _selectedResourceEntity;
            }
            set
            {
                if (SetProperty(ref _selectedResourceEntity, value, nameof(SelectedResourceEntity)) && (value != null))
                {
                    _namespace = GetLocalNamespace(((DteProjectFile)value.NeutralProjectFile)?.DefaultProjectItem);
                    UpdateReplacements();
                }
            }
        }

        [Required(AllowEmptyStrings = false)]
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                if (SetProperty(ref _key, value, nameof(Key)))
                {
                    UpdateReplacements();
                }
            }
        }

        public ICollection<string> Replacements => _replacements;

        [Required(AllowEmptyStrings = false)]
        public string Replacement
        {
            get
            {
                return _replacement;
            }
            set
            {
                SetProperty(ref _replacement, value, nameof(Replacement));
            }
        }

        [Required(AllowEmptyStrings = false)]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                SetProperty(ref _value, value, nameof(Value));
            }
        }

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                SetProperty(ref _comment, value, nameof(Comment));
            }
        }

        protected override IEnumerable<string> GetDataErrors(string propertyName)
        {
            return GetKeyErrors(propertyName).Concat(base.GetDataErrors(propertyName));
        }

        private IEnumerable<string> GetKeyErrors(string propertyName)
        {
            if (!string.Equals(propertyName, nameof(Key)))
                yield break;

            if (string.IsNullOrEmpty(Key))
                yield break;

            if (!Key.All(c => (c == '_') || char.IsLetterOrDigit(c)) || char.IsDigit(Key.FirstOrDefault()))
                yield return "The key contains invalid characters.";

            var selectedResourceEntity = _selectedResourceEntity;
            if (selectedResourceEntity == null)
                yield break;

            if (!selectedResourceEntity.Entries.Any(entry => string.Equals(entry.Key, Key, StringComparison.OrdinalIgnoreCase)))
                yield break;

            yield return "Duplicate key";
        }

        private string GetLocalNamespace(ProjectItem resxItem)
        {
            try
            {
                if (resxItem == null)
                    return string.Empty;

                var resxPath = resxItem.FileNames[0];
                var resxFolder = Path.GetDirectoryName(resxPath);
                var project = resxItem.ContainingProject;
                var projectFolder = Path.GetDirectoryName(project?.FullName);
                var rootNamespace = project?.Properties?.Item("RootNamespace")?.Value?.ToString();

                if ((resxFolder == null) || (projectFolder == null))
                    return string.Empty;

                var localNamespace = rootNamespace;
                if (resxFolder.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
                {
                    localNamespace += resxFolder.Substring(projectFolder.Length).Replace('\\', '.');
                }

                return localNamespace;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void UpdateReplacements()
        {
            _replacements.Clear();
            _replacements.AddRange(_patterns.Select(EvaluatePattern));

            Replacement = _replacements.FirstOrDefault();
        }

        private string EvaluatePattern(string pattern)
        {
            Contract.Requires(pattern != null);

            return pattern.Replace("$File", SelectedResourceEntity?.BaseName).Replace("$Key", Key).Replace("$Namespace", _namespace);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_patterns != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_replacements != null);
        }
    }
}
