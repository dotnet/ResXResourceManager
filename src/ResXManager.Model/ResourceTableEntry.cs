namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    using AutoProperties;

    using PropertyChanged;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    using Throttle;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Represents one entry in the resource table.
    /// </summary>
    public sealed class ResourceTableEntry : INotifyPropertyChanged, IDataErrorInfo
    {
        private static readonly Regex _duplicateKeyExpression = new(@"_Duplicate\[\d+\]$");
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;

        // the key actually stored in the file, identical to Key if no error occurred.
        private string _storedKey;

        // the last validation error
        private string? _keyValidationError;

        private ISet<string>? _mutedRuleIds;

        private IConfiguration Configuration => Container.Container.Configuration;

        /// <summary>
        /// A reference to the rules that are enabled in the configuration.
        /// </summary>
        private ResourceTableEntryRules Rules => Configuration.Rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTableEntry" /> class.
        /// </summary>
        /// <param name="container">The owner.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="index">The original index of the resource in the file.</param>
        /// <param name="languages">The localized values.</param>
        internal ResourceTableEntry(ResourceEntity container, string key, double index, IDictionary<CultureKey, ResourceLanguage> languages)
        {
            Container = container;
            _storedKey = key;

            Key.SetBackingField(key);
            Index.SetBackingField(index);

            _languages = languages;

            Values = new ResourceTableValues<string?>(_languages, lang => lang.GetValue(Key), (lang, value) => lang.SetValue(Key, value));
            Values.ValueChanged += Values_ValueChanged;

            Comments = new ResourceTableValues<string?>(_languages, lang => lang.GetComment(Key), (lang, value) => lang.SetComment(Key, value));
            Comments.ValueChanged += Comments_ValueChanged;

            FileExists = new ResourceTableValues<bool>(_languages, _ => true, (_, _) => false);

            SnapshotValues = new ResourceTableValues<string?>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Text, (_, _) => false);
            SnapshotComments = new ResourceTableValues<string?>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Comment, (_, _) => false);

            ValueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (_, _) => false);
            CommentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (_, _) => false);

            IsItemInvariant = new ResourceTableValues<bool>(_languages, lang => GetIsInvariant(lang.CultureKey), (lang, value) => SetIsInvariant(lang.CultureKey, value));
            TranslationState = new ResourceTableValues<TranslationState>(_languages, lang => GetTranslationState(lang.CultureKey), (lang, value) => SetTranslationState(lang.CultureKey, value));

            IsRuleEnabled = new DelegateIndexer<string, bool>(GetIsRuleEnabled, SetIsRuleEnabled);
        }

        private void ResetTableValues()
        {
            Values.ValueChanged -= Values_ValueChanged;
            Values = new ResourceTableValues<string?>(_languages, lang => lang.GetValue(Key), (lang, value) => lang.SetValue(Key, value));
            Values.ValueChanged += Values_ValueChanged;

            Comments.ValueChanged -= Comments_ValueChanged;
            Comments = new ResourceTableValues<string?>(_languages, lang => lang.GetComment(Key), (lang, value) => lang.SetComment(Key, value));
            Comments.ValueChanged += Comments_ValueChanged;

            FileExists = new ResourceTableValues<bool>(_languages, _ => true, (_, _) => false);

            SnapshotValues = new ResourceTableValues<string?>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Text, (_, _) => false);
            SnapshotComments = new ResourceTableValues<string?>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Comment, (_, _) => false);

            ValueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (_, _) => false);
            CommentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (_, _) => false);

            IsItemInvariant = new ResourceTableValues<bool>(_languages, lang => GetIsInvariant(lang.CultureKey), (lang, value) => SetIsInvariant(lang.CultureKey, value));
            TranslationState = new ResourceTableValues<TranslationState>(_languages, lang => GetTranslationState(lang.CultureKey), (lang, value) => SetTranslationState(lang.CultureKey, value));
        }

        internal void Update(int index)
        {
            UpdateIndex(index);

            ResetTableValues();

            Refresh();
        }

        public ResourceEntity Container { get; }

        /// <summary>
        /// Gets the key of the resource.
        /// </summary>
        [OnChangedMethod(nameof(OnKeyChanged))]
        public string Key { get; set; } = string.Empty;
        private void OnKeyChanged()
        {
            _keyValidationError = null;

            var value = Key;

            if (_storedKey == value)
                return;

            var resourceLanguages = _languages.Values;
            if (!resourceLanguages.All(language => language.CanEdit()))
            {
                _keyValidationError = Resources.NotAllLanguagesAreEditable;
                return;
            }

            if (resourceLanguages.Any(language => language.KeyExists(value)))
            {
                _keyValidationError = string.Format(CultureInfo.CurrentCulture, Resources.KeyAlreadyExists, value);
                return;
            }

            foreach (var language in resourceLanguages)
            {
                language.RenameKey(_storedKey, value);
            }

            ResetTableValues();

            _storedKey = value;
        }

        public ResourceLanguage NeutralLanguage => _languages.First().Value;

        /// <summary>
        /// Gets or sets the comment of the neutral language.
        /// </summary>
        [DependsOn(nameof(Comments))]
        public string? Comment
        {
            get => NeutralLanguage.GetComment(Key) ?? string.Empty;
            set => NeutralLanguage.SetComment(Key, value);
        }

        /// <summary>
        /// Gets the localized values.
        /// </summary>
        public ResourceTableValues<string?> Values { get; private set; }

        /// <summary>
        /// Gets the localized comments.
        /// </summary>
        public ResourceTableValues<string?> Comments { get; private set; }

        [DependsOn(nameof(Snapshot))]
        public ResourceTableValues<string?> SnapshotValues { get; private set; }

        [DependsOn(nameof(Snapshot))]
        public ResourceTableValues<string?> SnapshotComments { get; private set; }

        public ResourceTableValues<bool> FileExists { get; private set; }

        [DependsOn(nameof(Snapshot))]
        public ResourceTableValues<ICollection<string>> ValueAnnotations { get; private set; }

        [DependsOn(nameof(Snapshot))]
        public ResourceTableValues<ICollection<string>> CommentAnnotations { get; private set; }

        [DependsOn(nameof(Comment))]
        public ICollection<CultureKey> Languages => _languages.Keys;

        public ResourceTableValues<bool> IsItemInvariant { get; private set; }

        public ResourceTableValues<TranslationState> TranslationState { get; private set; }

        [DependsOn(nameof(Comment))]
        public DelegateIndexer<string, bool> IsRuleEnabled { get; }

        // TODO: maybe rules should be mutable per language, like Invariant?
        private ISet<string> MutedRuleIds => _mutedRuleIds ??= new HashSet<string>(GetMutedRuleIds(CultureKey.Neutral), StringComparer.OrdinalIgnoreCase);

        private IEnumerable<string> GetMutedRuleIds(CultureKey culture)
        {
            var comment = Comments.GetValue(culture);

            return ResourceTableEntryRules.GetMutedRuleIds(comment);
        }

        private bool GetIsRuleEnabled(string? ruleId)
        {
            // ! Collection.Contains allows null arguments
            return !MutedRuleIds.Contains(ruleId!) && Rules.IsEnabled(ruleId);
        }

        private void SetIsRuleEnabled(string? ruleId, bool value)
        {
            if (ruleId == null)
                return;

            var ids = new HashSet<string>(MutedRuleIds, StringComparer.OrdinalIgnoreCase);

            if (value)
            {
                ids.Remove(ruleId);
            }
            else
            {
                ids.Add(ruleId);
            }

            SetMutedRuleIds(CultureKey.Neutral, ids);
        }

        private void SetMutedRuleIds(CultureKey culture, ISet<string> mutedRuleIds)
        {
            var comment = Comments.GetValue(culture);

            var newComment = ResourceTableEntryRules.SetMutedRuleIds(comment, mutedRuleIds);

            if (!CanEdit(culture))
                return;

            Comments.SetValue(culture, newComment);

            Refresh();
        }

        [DependsOn(nameof(Comment))]
        public bool IsInvariant
        {
            get => GetIsInvariant(CultureKey.Neutral);
            set => SetIsInvariant(CultureKey.Neutral, value);
        }

        private bool GetIsInvariant(CultureKey culture)
        {
            return Comments.GetValue(culture).GetIsInvariant();
        }

        private bool SetIsInvariant(CultureKey culture, bool value)
        {
            var comment = Comments.GetValue(culture).WithIsInvariant(value);

            if (!Comments.SetValue(culture, comment))
                return false;

            Refresh();
            return true;
        }

        private TranslationState GetTranslationState(CultureKey culture)
        {
            var value = Values.GetValue(culture);
            var comment = Comments.GetValue(culture);

            return GetTranslationState(value, comment);
        }

        private static TranslationState GetTranslationState(string? value, string? comment)
        {
            var state = comment.GetTranslationState();

            return state ?? (string.IsNullOrEmpty(value) ? Model.TranslationState.New : Model.TranslationState.Approved);
        }

        private bool SetTranslationState(CultureKey culture, TranslationState? state)
        {
            var value = Values.GetValue(culture);
            var comment = Comments.GetValue(culture) ?? string.Empty;

            if (state == Model.TranslationState.New)
            {
                if (string.IsNullOrEmpty(value))
                {
                    state = null;
                }
            }
            else if (state == Model.TranslationState.Approved)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    state = null;
                }
            }

            var newComment = comment.WithTranslationState(state);

            if (!Comments.SetValue(culture, newComment))
                return false;

            Refresh();
            return true;
        }

        public string? GetCommentText(CultureKey culture)
        {
            var comment = Comments.GetValue(culture);

            comment.DecomposeCommentTokens(out var text, out _, out _);

            return text;
        }

        public void SetCommentText(CultureKey culture, string? value)
        {
            var comment = Comments.GetValue(culture);

            comment.DecomposeCommentTokens(out _, out var state, out var isInvariant);

            value = value.WithIsInvariant(isInvariant).WithTranslationState(state)?.Trim();

            Comments.SetValue(culture, value);
        }

        [DependsOn(nameof(Key))]
        public bool IsDuplicateKey => _duplicateKeyExpression.Match(Key).Success;

        public ReadOnlyCollection<CodeReference>? CodeReferences { get; set; }

        [OnChangedMethod(nameof(OnIndexChanged))]
        public double Index { get; set; }
        private void OnIndexChanged()
        {
            Container.OnIndexChanged(this);
        }

        /// <summary>
        /// Updates the index to it's actual value only, without trying to adjust the file content.
        /// </summary>
        /// <param name="value">The value.</param>
        internal void UpdateIndex(double value)
        {
            if (Math.Abs(value - Index) <= double.Epsilon)
                return;

            Index.SetBackingField(value);
            OnPropertyChanged(nameof(Index));
        }

#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<CultureKey, ResourceData>? Snapshot { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        public bool CanEdit(CultureKey? cultureKey)
        {
            return Container.CanEdit(cultureKey);
        }

        public void Refresh()
        {
            OnValuesChanged();
            OnCommentsChanged();
        }

        public bool HasRulesMismatches(IEnumerable<object> cultures)
        {
            var neutralValue = Values.GetValue(null);
            var values = cultures.Select(CultureKey.Parse)
                .Where(lang => !lang.IsNeutral)
                .Select(lang => Values.GetValue(lang))
                .Where(value => !value.IsNullOrEmpty())
                .ToList()
                .AsReadOnly();

            return !Rules.CompliesToRules(MutedRuleIds, neutralValue, values, out _);
        }

        public bool HasSnapshotDifferences(IEnumerable<object> cultures)
        {
            return Snapshot != null && cultures.Select(CultureKey.Parse).Any(IsSnapshotDifferent);
        }

        private bool IsSnapshotDifferent(CultureKey culture)
        {
            if (Snapshot == null)
                return false;

            var snapshotValue = Snapshot.GetValueOrDefault(culture)?.Text ?? string.Empty;
            var currentValue = Values.GetValue(culture) ?? string.Empty;

            var snapshotComment = Snapshot.GetValueOrDefault(culture)?.Comment ?? string.Empty;
            var currentComment = Comments.GetValue(culture) ?? string.Empty;

            return !string.Equals(snapshotValue, currentValue, StringComparison.Ordinal) || !string.Equals(snapshotComment, currentComment, StringComparison.Ordinal);
        }

        private void Values_ValueChanged(object? sender, EventArgs e)
        {
            OnValuesChanged();
        }

        [Throttled(typeof(AsyncThrottle))]
        private void OnValuesChanged()
        {
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(FileExists));
            OnPropertyChanged(nameof(ValueAnnotations));
        }

        private void Comments_ValueChanged(object? sender, EventArgs e)
        {
            // just clear and re-fetch upon next usage
            _mutedRuleIds = null;

            OnCommentsChanged();
        }

        [Throttled(typeof(AsyncThrottle))]
        private void OnCommentsChanged()
        {
            OnPropertyChanged(nameof(Comment));
            OnPropertyChanged(nameof(Comments));
            OnPropertyChanged(nameof(IsInvariant));
            OnPropertyChanged(nameof(IsItemInvariant));
            OnPropertyChanged(nameof(CommentAnnotations));
            OnPropertyChanged(nameof(IsRuleEnabled));
            OnPropertyChanged(nameof(TranslationState));
        }

        private ICollection<string> GetValueAnnotations(ResourceLanguage language)
        {
            var cultureKey = language.CultureKey;

            var value = Values.GetValue(cultureKey);

            return GetRuleAnnotations(language)
                .Concat(GetSnapshotDifferences(language, value, d => d?.Text))
                .Concat(GetInvariantMismatches(cultureKey, value))
                .ToArray();
        }

        public bool GetError(CultureKey culture, out string? errorMessage)
        {
            errorMessage = null;

            var value = Values.GetValue(culture);

            var isInvariant = IsInvariant || IsItemInvariant.GetValue(culture);

            if (value.IsNullOrEmpty())
            {
                if (!isInvariant)
                {
                    errorMessage = GetErrorPrefix(culture) + Resources.ResourceTableEntry_Error_MissingTranslation;
                    return true;
                }
            }
            else
            {
                if (culture == CultureKey.Neutral)
                    return false;

                if (isInvariant)
                {
                    errorMessage = GetErrorPrefix(culture) + Resources.ResourceTableEntry_Error_InvariantWithValue;
                    return true;
                }

                var neutralValue = NeutralLanguage.GetValue(Key);
                if (neutralValue.IsNullOrEmpty())
                    return false;

                if (Rules.CompliesToRules(MutedRuleIds, neutralValue, value, out var ruleMessages))
                    return false;

                errorMessage = GetErrorPrefix(culture) + string.Join(" ", ruleMessages);
                return true;
            }

            return false;
        }

        private string GetErrorPrefix(CultureKey culture)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}{1}: ", Key, culture);
        }

        private IEnumerable<string> GetInvariantMismatches(CultureKey culture, string? value)
        {
            if (culture == CultureKey.Neutral)
                yield break;

            var isInvariant = IsInvariant || IsItemInvariant.GetValue(culture);

            if (isInvariant && !value.IsNullOrEmpty())
                yield return Resources.ResourceTableEntry_Error_InvariantWithValue;
        }

        private ICollection<string> GetCommentAnnotations(ResourceLanguage language)
        {
            return GetSnapshotDifferences(language, Comments.GetValue(language.CultureKey), d => d?.Comment)
                .ToArray();
        }

        private IEnumerable<string> GetSnapshotDifferences(ResourceLanguage language, string? current, Func<ResourceData?, string?> selector)
        {
            var snapshot = Snapshot;
            if (snapshot == null)
                yield break;

            var snapshotData = snapshot.GetValueOrDefault(language.CultureKey);

            var snapshotValue = selector(snapshotData) ?? string.Empty;
            if (snapshotValue.Equals(current ?? string.Empty, StringComparison.Ordinal))
                yield break;

            yield return string.Format(CultureInfo.CurrentCulture, Resources.SnapshotAnnotation, snapshotValue);
        }

        private IEnumerable<string> GetRuleAnnotations(ResourceLanguage language)
        {
            if (language.IsNeutralLanguage)
                yield break;

            var value = language.GetValue(Key);
            if (value.IsNullOrEmpty())
                yield break;

            var neutralValue = NeutralLanguage.GetValue(Key);
            if (neutralValue.IsNullOrEmpty())
                yield break;

            if (Rules.CompliesToRules(MutedRuleIds, neutralValue, value, out var ruleMessages))
                yield break;

            foreach (var message in ruleMessages)
                yield return message;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string? IDataErrorInfo.this[string? columnName] => columnName != nameof(Key) ? null : _keyValidationError;

        string? IDataErrorInfo.Error => null;
    }
}