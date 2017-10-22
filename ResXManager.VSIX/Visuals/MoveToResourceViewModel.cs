namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
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
        private readonly string _extension;

        public MoveToResourceViewModel([NotNull, ItemNotNull] ICollection<string> patterns, [NotNull][ItemNotNull] ICollection<ResourceEntity> resourceEntities, [NotNull] string text, [NotNull] string extension, string className, string functionName)
        {
            Contract.Requires(patterns != null);
            Contract.Requires(resourceEntities != null);
            Contract.Requires(text != null);
            Contract.Requires(extension != null);

            ResourceEntities = resourceEntities;
            SelectedResourceEntity = resourceEntities.FirstOrDefault();

            ExistingEntries = resourceEntities
                .SelectMany(entity => entity.Entries)
                .Where(entry => entry.Values[null] == text)
                .ToArray();
            ReuseExisiting = ExistingEntries.Any();

            SelectedResourceEntry = ExistingEntries.FirstOrDefault();
            _extension = extension;

            Replacements = patterns.Select(p => new Replacement(p, EvaluatePattern)).ToArray();
            Key = CreateKey(text, className, functionName);
            Value = text;
        }

        [NotNull]
        [ItemNotNull]
        public ICollection<ResourceEntity> ResourceEntities { get; }

        [Required]
        public ResourceEntity SelectedResourceEntity { get; set; }

        public ResourceTableEntry SelectedResourceEntry { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DependsOn(nameof(ReuseExisiting), nameof(SelectedResourceEntity))] // must raise a change event for key, key validation is different when these change
        public string Key { get; set; }

        [NotNull, ItemNotNull]
        public ICollection<Replacement> Replacements { get; }

        [CanBeNull]
        [Required]
        public Replacement Replacement { get; set; }

        [CanBeNull]
        [Required(AllowEmptyStrings = false)]
        public string Value { get; set; }

        [CanBeNull]
        public string Comment { get; set; }

        public bool ReuseExisiting { get; set; }

        [NotNull]
        [ItemNotNull]
        public ICollection<ResourceTableEntry> ExistingEntries { get; }

        public int SelectedReplacementIndex
        {
            get => Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension];
            set => Settings.Default.MoveToResourcePreferedReplacementPatternIndex[_extension] = value;
        }

        [CanBeNull]
        private string GetKeyErrors([CanBeNull] string propertyName)
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

        private bool KeyExists([CanBeNull] string value)
        {
            return SelectedResourceEntity?.Entries.Any(entry => string.Equals(entry.Key, value, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        [NotNull]
        private static string GetLocalNamespace([CanBeNull] ProjectItem resxItem)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            try
            {
                var resxPath = resxItem?.TryGetFileName();
                if (resxPath == null)
                    return string.Empty;

                var resxFolder = Path.GetDirectoryName(resxPath);
                var project = resxItem.ContainingProject;
                var projectFolder = Path.GetDirectoryName(project?.FullName);
                var rootNamespace = project?.Properties?.Item(@"RootNamespace")?.Value?.ToString() ?? string.Empty;

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
            Replacements.ForEach(r => r.Update());
        }

        [NotNull]
        private string EvaluatePattern([NotNull] string pattern)
        {
            Contract.Requires(pattern != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var entity = ReuseExisiting ? SelectedResourceEntry?.Container : SelectedResourceEntity;
            var key = ReuseExisiting ? SelectedResourceEntry?.Key : Key;
            var localNamespace = GetLocalNamespace(((DteProjectFile)entity?.NeutralProjectFile)?.DefaultProjectItem);

            return pattern.Replace(@"$File", entity?.BaseName)
                .Replace(@"$Key", key)
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
            Update();
        }

        string IDataErrorInfo.this[string columnName] => GetKeyErrors(columnName);

        string IDataErrorInfo.Error => null;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(ResourceEntities != null);
            Contract.Invariant(Replacements != null);
            Contract.Invariant(_extension != null);
        }
    }

    public sealed class Replacement : INotifyPropertyChanged
    {
        [NotNull]
        private readonly string _pattern;
        [NotNull]
        private readonly Func<string, string> _evaluator;

        public Replacement([NotNull] string pattern, [NotNull] Func<string, string> evaluator)
        {
            Contract.Requires(pattern != null);
            Contract.Requires(evaluator != null);
            _pattern = pattern;
            _evaluator = evaluator;
        }

        [CanBeNull]
        public string Value => _evaluator(_pattern);

        public event PropertyChangedEventHandler PropertyChanged;

        public void Update()
        {
            OnPropertyChanged(nameof(Value));
        }

        private void OnPropertyChanged([NotNull] string propertyName)
        {
            Contract.Requires(propertyName != null);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_pattern != null);
            Contract.Invariant(_evaluator != null);
        }
    }
}
