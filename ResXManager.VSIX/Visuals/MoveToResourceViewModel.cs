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
    using System.Text;

    using EnvDTE;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class MoveToResourceViewModel : ObservableObject
    {
        private readonly ICollection<string> _patterns;
        private readonly ICollection<ResourceTableEntry> _existingEntries;
        private readonly ICollection<ResourceEntity> _resourceEntities;
        private readonly ObservableCollection<string> _replacements = new ObservableCollection<string>();
        private ResourceEntity _selectedResourceEntity;
        private ResourceTableEntry _selectedResourceEntry;
        private string _key;
        private string _value;
        private string _comment;
        private string _replacement;
        private bool _reuseExisiting;

        public MoveToResourceViewModel(ICollection<string> patterns, ICollection<ResourceEntity> resourceEntities, string text)
        {
            Contract.Requires(patterns != null);
            Contract.Requires(resourceEntities != null);
            Contract.Requires(text != null);

            _patterns = patterns;
            _resourceEntities = resourceEntities;
            _existingEntries = resourceEntities
                .SelectMany(entity => entity.Entries)
                .Where(entry => entry.Values[null] == text)
                .ToArray();

            _reuseExisiting = _existingEntries.Any();
            _selectedResourceEntry = _existingEntries.FirstOrDefault();
            _value = text;

            if (!_reuseExisiting)
                _key = CreateKey(text);
        }

        public ICollection<ResourceEntity> ResourceEntities => _resourceEntities;

        [Required]
        public ResourceEntity SelectedResourceEntity
        {
            get
            {
                return _selectedResourceEntity;
            }
            set
            {
                if (SetProperty(ref _selectedResourceEntity, value, nameof(SelectedResourceEntity)))
                {
                    Update();
                }
            }
        }

        public ResourceTableEntry SelectedResourceEntry
        {
            get
            {
                return _selectedResourceEntry;
            }
            set
            {
                if (SetProperty(ref _selectedResourceEntry, value, nameof(SelectedResourceEntry)))
                {
                    Update();
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
                    Update();
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

        public bool ReuseExisiting
        {
            get
            {
                return _reuseExisiting;
            }
            set
            {
                if (SetProperty(ref _reuseExisiting, value, nameof(ReuseExisiting)))
                {
                    if (value)
                    {
                        Key = _selectedResourceEntry?.Key;
                        Comment = _selectedResourceEntry?.Comment;
                    }
                    else
                    {
                        Comment = string.Empty;
                    }

                    Update();
                }
            }
        }

        public ICollection<ResourceTableEntry> ExistingEntries => _existingEntries;

        protected override IEnumerable<string> GetDataErrors(string propertyName)
        {
            return GetKeyErrors(propertyName).Concat(base.GetDataErrors(propertyName));
        }

        private IEnumerable<string> GetKeyErrors(string propertyName)
        {
            if (ReuseExisiting)
                yield break;

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

        private static string GetLocalNamespace(ProjectItem resxItem)
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

        private void Update()
        {
            _replacements.Clear();
            _replacements.AddRange(_patterns.Select(EvaluatePattern));

            Replacement = _replacements.FirstOrDefault();

            if (ReuseExisiting)
            {
                Key = _selectedResourceEntry?.Key;
                Value = _selectedResourceEntry?.Values[null];
                Comment = _selectedResourceEntry?.Comment;
            }

            OnPropertyChanged(nameof(Key)); // to force new validation...
        }

        private string EvaluatePattern(string pattern)
        {
            Contract.Requires(pattern != null);

            var entity = ReuseExisiting ? _selectedResourceEntry?.Container : _selectedResourceEntity;

            var localNamespace = GetLocalNamespace(((DteProjectFile)entity?.NeutralProjectFile)?.DefaultProjectItem);

            return pattern.Replace("$File", SelectedResourceEntity?.BaseName).Replace("$Key", Key).Replace("$Namespace", localNamespace);
        }

        private static string CreateKey(string text)
        {
            var key = text?.Aggregate(new StringBuilder(), (builder, c) => builder.Append(IsCharValidForSymbol(c) ? c : '_'))?.ToString() ?? "_";

            if (!IsCharValidForSymbolStart(key.FirstOrDefault()))
                key = "_" + key;

            return key;
        }

        private static bool IsCharValidForSymbol(char c)
        {
            return (c == '_') || char.IsLetterOrDigit(c);
        }

        private static bool IsCharValidForSymbolStart(char c)
        {
            return (c == '_') || char.IsLetter(c);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_patterns != null);
            Contract.Invariant(_resourceEntities != null);
            Contract.Invariant(_replacements != null);
        }
    }
}
