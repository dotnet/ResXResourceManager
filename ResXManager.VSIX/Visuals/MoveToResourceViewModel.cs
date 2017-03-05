namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Threading;

    using EnvDTE;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.VSIX.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class MoveToResourceViewModel : ObservableObject
    {
        [NotNull]
        private readonly ICollection<string> _patterns;
        private readonly ICollection<ResourceTableEntry> _existingEntries;
        [NotNull]
        private readonly ICollection<ResourceEntity> _resourceEntities;
        [NotNull]
        private readonly ObservableCollection<string> _replacements = new ObservableCollection<string>();
        [NotNull]
        private readonly string _extension;

        private ResourceEntity _selectedResourceEntity;
        private ResourceTableEntry _selectedResourceEntry;
        private string _key;
        private string _value;
        private string _comment;
        private string _replacement;
        private bool _reuseExisiting;
        private int _selectedReplacementIndex;
        private bool _isUpdating;

        public MoveToResourceViewModel([NotNull] ICollection<string> patterns, [NotNull] ICollection<ResourceEntity> resourceEntities, [NotNull] string text, [NotNull] string extension, string className, string functionName)
        {
            Contract.Requires(patterns != null);
            Contract.Requires(resourceEntities != null);
            Contract.Requires(text != null);
            Contract.Requires(extension != null);

            _patterns = patterns;
            _resourceEntities = resourceEntities;
            _existingEntries = resourceEntities
                .SelectMany(entity => entity.Entries)
                .Where(entry => entry.Values[null] == text)
                .ToArray();

            _reuseExisiting = _existingEntries.Any();
            _selectedResourceEntry = _existingEntries.FirstOrDefault();
            _value = text;
            _extension = extension;

            if (!_reuseExisiting)
                _key = CreateKey(text, className, functionName);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => OnPropertyChanged(nameof(Key)));
        }

        [NotNull]
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

        [PropertyDependency(nameof(ReuseExisiting), nameof(SelectedResourceEntity))]
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

        [NotNull]
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

        public int SelectedReplacementIndex
        {
            get
            {
                return _selectedReplacementIndex;
            }
            set
            {
                if (SetProperty(ref _selectedReplacementIndex, value, nameof(SelectedReplacementIndex)) && !_isUpdating)
                {
                    Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension] = value;
                }
            }
        }

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

            var key = Key;

            if (string.IsNullOrEmpty(key))
                yield break;

            if (!key.All(c => (c == '_') || char.IsLetterOrDigit(c)) || char.IsDigit(key.FirstOrDefault()))
                yield return Resources.KeyContainsInvalidCharacters;

            if (KeyExists(key))
                yield return Resources.DuplicateKey;
        }

        private bool KeyExists(string value)
        {
            return _selectedResourceEntity?.Entries.Any(entry => string.Equals(entry.Key, value, StringComparison.OrdinalIgnoreCase)) ?? false;
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
                var rootNamespace = project?.Properties?.Item(@"RootNamespace")?.Value?.ToString();

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
            _isUpdating = true;

            _replacements.Clear();
            _replacements.AddRange(_patterns.Select(EvaluatePattern));

            SelectedReplacementIndex = Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension];
            Replacement = _replacements.Skip(SelectedReplacementIndex).FirstOrDefault() ?? _replacements.FirstOrDefault();

            if (ReuseExisiting)
            {
                Key = _selectedResourceEntry?.Key;
                Value = _selectedResourceEntry?.Values[null];
                Comment = _selectedResourceEntry?.Comment;
            }

            Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(Key))); // to force new validation...

            _isUpdating = false;
        }

        [NotNull]
        private string EvaluatePattern([NotNull] string pattern)
        {
            Contract.Requires(pattern != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var entity = ReuseExisiting ? _selectedResourceEntry?.Container : _selectedResourceEntity;

            var localNamespace = GetLocalNamespace(((DteProjectFile)entity?.NeutralProjectFile)?.DefaultProjectItem);

            return pattern.Replace(@"$File", SelectedResourceEntity?.BaseName)
                .Replace(@"$Key", Key)
                .Replace(@"$Namespace", localNamespace);
        }

        [NotNull]
        private static string CreateKey([NotNull] string text, string className, string functionName)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var keyBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(className))
                keyBuilder.AppendFormat(@"{0}_", className);
            if (!string.IsNullOrEmpty(functionName))
                keyBuilder.AppendFormat(@"{0}_", functionName);

            var makeUpper = true;

            foreach (var c in text)
            {
                if (!IsCharValidForSymbol(c))
                {
                    makeUpper = true;
                }
                else
                {
                    keyBuilder.Append(makeUpper ? char.ToUpper(c, CultureInfo.CurrentCulture) : c);
                    makeUpper = false;
                }
            }

            var key = keyBuilder.ToString();

            if (!IsCharValidForSymbolStart(key.FirstOrDefault()))
                key = @"_" + key;

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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_patterns != null);
            Contract.Invariant(_resourceEntities != null);
            Contract.Invariant(_replacements != null);
            Contract.Invariant(_extension != null);
        }
    }
}
