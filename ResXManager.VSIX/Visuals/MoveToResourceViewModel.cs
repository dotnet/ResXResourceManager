namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;

    using EnvDTE;

    using JetBrains.Annotations;

    using PropertyChanged;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.VSIX.Properties;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class MoveToResourceViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        [NotNull]
        private readonly ICollection<string> _patterns;
        [NotNull]
        private readonly string _extension;

        private bool _isUpdating;
        public MoveToResourceViewModel([NotNull] ICollection<string> patterns, [NotNull] ICollection<ResourceEntity> resourceEntities, [NotNull] string text, [NotNull] string extension, string className, string functionName)
        {
            Contract.Requires(patterns != null);
            Contract.Requires(resourceEntities != null);
            Contract.Requires(text != null);
            Contract.Requires(extension != null);

            _patterns = patterns;
            ResourceEntities = resourceEntities;
            SelectedResourceEntity = resourceEntities.FirstOrDefault();

            ExistingEntries = resourceEntities
                .SelectMany(entity => entity.Entries)
                .Where(entry => entry.Values[null] == text)
                .ToArray();
            ReuseExisiting = ExistingEntries.Any();
            SelectedResourceEntry = ExistingEntries.FirstOrDefault();
            _extension = extension;

            Key = CreateKey(text, className, functionName);
        }

        [NotNull]
        public ICollection<ResourceEntity> ResourceEntities { get; }

        [Required]
        public ResourceEntity SelectedResourceEntity { get; set; }

        [UsedImplicitly]
        private void OnSelectedResourceEntityChanged()
        {
            Update();
        }

        public ResourceTableEntry SelectedResourceEntry { get; set; }

        [UsedImplicitly]
        private void OnSelectedResourceEntryChanged()
        {
            Update();
        }

        [Required(AllowEmptyStrings = false)]
        [DependsOn(nameof(ReuseExisiting), nameof(SelectedResourceEntity))] // key validation is different when these change
        public string Key { get; set; }

        [UsedImplicitly]
        private void OnKeyChanged()
        {
            Update();
        }

        [NotNull]
        public ObservableCollection<string> Replacements { get; } = new ObservableCollection<string>();

        [Required(AllowEmptyStrings = false)]
        public string Replacement { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Value { get; set; }

        public string Comment { get; set; }

        public bool ReuseExisiting { get; set; }

        [UsedImplicitly]
        private void OnReuseExisitingChanged()
        {
            if (!ReuseExisiting)
            {
                Comment = string.Empty;
            }
            else
            {
                Key = SelectedResourceEntry?.Key;
                Value = SelectedResourceEntry?.Values[null];
                Comment = SelectedResourceEntry?.Comment;
            }

            Update();
        }

        public ICollection<ResourceTableEntry> ExistingEntries { get; }

        public int SelectedReplacementIndex
        {
            get => Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension];
            set
            {
                if (!_isUpdating)
                {
                    Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension] = value;
                }
            }
        }

        [CanBeNull]
        private string GetKeyErrors(string propertyName)
        {
            if (ReuseExisiting)
                return null;

            if (!string.Equals(propertyName, nameof(Key)))
                return null;

            var key = Key;

            if (string.IsNullOrEmpty(key))
                return null;

            if (!key.All(c => (c == '_') || char.IsLetterOrDigit(c)) || char.IsDigit(key.FirstOrDefault()))
                return Resources.KeyContainsInvalidCharacters;

            if (KeyExists(key))
                return Resources.DuplicateKey;

            return null;
        }

        private bool KeyExists(string value)
        {
            return SelectedResourceEntity?.Entries.Any(entry => string.Equals(entry.Key, value, StringComparison.OrdinalIgnoreCase)) ?? false;
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

        [Throttled(typeof(DispatcherThrottle))]
        private void Update()
        {
            _isUpdating = true;

            Replacements.Clear();
            Replacements.AddRange(_patterns.Select(EvaluatePattern));
            Replacement = Replacements.Skip(SelectedReplacementIndex).FirstOrDefault() ?? Replacements.FirstOrDefault();

            _isUpdating = false;
        }

        [NotNull]
        private string EvaluatePattern([NotNull] string pattern)
        {
            Contract.Requires(pattern != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var entity = ReuseExisiting ? SelectedResourceEntry?.Container : SelectedResourceEntity;

            var localNamespace = GetLocalNamespace(((DteProjectFile)entity?.NeutralProjectFile)?.DefaultProjectItem);

            return pattern.Replace(@"$File", entity?.BaseName)
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator, UsedImplicitly]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string IDataErrorInfo.this[string columnName] => GetKeyErrors(columnName);

        string IDataErrorInfo.Error => null;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_patterns != null);
            Contract.Invariant(ResourceEntities != null);
            Contract.Invariant(Replacements != null);
            Contract.Invariant(_extension != null);
        }
    }
}
