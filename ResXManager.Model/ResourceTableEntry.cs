namespace tomenglertde.ResXManager.Model
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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Windows.Threading;

    using AutoProperties;

    using JetBrains.Annotations;

    using PropertyChanged;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Represents one entry in the resource table.
    /// </summary>
    public sealed class ResourceTableEntry : INotifyPropertyChanged, IDataErrorInfo
    {
        private const string InvariantKey = "@Invariant";

        [NotNull]
        private readonly Regex _duplicateKeyExpression = new Regex(@"_Duplicate\[\d+\]$");
        [NotNull]
        private IDictionary<CultureKey, ResourceLanguage> _languages;

        // the key actually stored in the file, identical to Key if no error occured.
        private string _storedKey;
        // the last validation error
        private string _keyValidationError;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTableEntry" /> class.
        /// </summary>
        /// <param name="container">The owner.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="index">The original index of the resource in the file.</param>
        /// <param name="languages">The localized values.</param>
        internal ResourceTableEntry([NotNull] ResourceEntity container, [NotNull] string key, double index, [NotNull] IDictionary<CultureKey, ResourceLanguage> languages)
        {
            Contract.Requires(container != null);
            Contract.Requires(!string.IsNullOrEmpty(key));
            Contract.Requires(languages != null);
            Contract.Requires(languages.Any());

            Container = container;
            _storedKey = key;

            Key.SetBackingField(key);
            Index.SetBackingField(index);

            _languages = languages;

            Values = new ResourceTableValues<string>(_languages, lang => lang.GetValue(Key), (lang, value) => lang.SetValue(Key, value));
            Values.ValueChanged += Values_ValueChanged;

            Comments = new ResourceTableValues<string>(_languages, lang => lang.GetComment(Key), (lang, value) => lang.SetComment(Key, value));
            Comments.ValueChanged += Comments_ValueChanged;

            FileExists = new ResourceTableValues<bool>(_languages, lang => true, (lang, value) => false);

            SnapshotValues = new ResourceTableValues<string>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Text, (lang, value) => false);
            SnapshotComments = new ResourceTableValues<string>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Comment, (lang, value) => false);

            ValueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (lang, value) => false);
            CommentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (lang, value) => false);

            Contract.Assume(languages.Any());
            var neutralLanguage = languages.First().Value;
            Contract.Assume(neutralLanguage != null);

            NeutralLanguage = neutralLanguage;
        }

        private void ResetTableValues()
        {
            Values.ValueChanged -= Values_ValueChanged;
            Values = new ResourceTableValues<string>(_languages, lang => lang.GetValue(Key), (lang, value) => lang.SetValue(Key, value));
            Values.ValueChanged += Values_ValueChanged;

            Comments.ValueChanged -= Comments_ValueChanged;
            Comments = new ResourceTableValues<string>(_languages, lang => lang.GetComment(Key), (lang, value) => lang.SetComment(Key, value));
            Comments.ValueChanged += Comments_ValueChanged;

            FileExists = new ResourceTableValues<bool>(_languages, lang => true, (lang, value) => false);

            SnapshotValues = new ResourceTableValues<string>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Text, (lang, value) => false);
            SnapshotComments = new ResourceTableValues<string>(_languages, lang => Snapshot?.GetValueOrDefault(lang.CultureKey)?.Comment, (lang, value) => false);

            ValueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (lang, value) => false);
            CommentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (lang, value) => false);
        }

        internal void Update(int index, [NotNull] IDictionary<CultureKey, ResourceLanguage> languages)
        {
            Contract.Requires(languages != null);
            Contract.Requires(languages.Any());

            var oldComment = Comment;

            Index = index;
            _languages = languages;

            var neutralLanguage = languages.First().Value;
            Contract.Assume(neutralLanguage != null);
            NeutralLanguage = neutralLanguage;

            ResetTableValues();

            // Preserve comments in WinForms designer resources, the designer always removes them.
            if (!string.IsNullOrEmpty(oldComment) && Container.IsWinFormsDesignerResource && string.IsNullOrEmpty(Comment))
            {
                Comment = oldComment;
            }

            Refresh();
        }

        internal void UpdateIndex(int index)
        {
            Index = index;
        }

        [NotNull]
        public ResourceEntity Container { get; }

        /// <summary>
        /// Gets the key of the resource.
        /// </summary>
        [NotNull]
        // ReSharper disable once MemberCanBePrivate.Global => Implicit bound to data grid.
        public string Key { get; set; } = string.Empty;

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnKeyChanged()
        {
            _keyValidationError = null;

            var value = Key;

            if (_storedKey == value)
                return;

            Contract.Assume(_storedKey != null);

            var resourceLanguages = _languages.Values;

            if (!resourceLanguages.All(language => language.CanEdit()))
            {
                _keyValidationError = "Not all languages are editable";
                return;
            }

            if (resourceLanguages.Any(language => language.KeyExists(value)))
            {
                _keyValidationError = "Key already exists: " + value;
                return;
            }

            foreach (var language in resourceLanguages)
            {
                Contract.Assume(language != null);
                language.RenameKey(_storedKey, value);
            }

            ResetTableValues();

            _storedKey = value;
        }

        [NotNull]
        public ResourceLanguage NeutralLanguage { get; private set; }

        /// <summary>
        /// Gets or sets the comment of the neutral language.
        /// </summary>
        [NotNull]
        public string Comment
        {
            get => NeutralLanguage.GetComment(Key) ?? string.Empty;
            set => NeutralLanguage.SetComment(Key, value);
        }

        /// <summary>
        /// Gets the localized values.
        /// </summary>
        [NotNull]
        public ResourceTableValues<string> Values { get; private set; }

        /// <summary>
        /// Gets the localized comments.
        /// </summary>
        [DependsOn(nameof(Comment))]
        [NotNull]
        public ResourceTableValues<string> Comments { get; private set; }

        [DependsOn(nameof(Snapshot))]
        [NotNull]
        public ResourceTableValues<string> SnapshotValues { get; private set; }

        [DependsOn(nameof(Snapshot))]
        [NotNull]
        public ResourceTableValues<string> SnapshotComments { get; private set; }

        [DependsOn(nameof(Values))]
        [NotNull]
        public ResourceTableValues<bool> FileExists { get; private set; }

        [DependsOn(nameof(Values), nameof(Snapshot))]
        [NotNull]
        public ResourceTableValues<ICollection<string>> ValueAnnotations { get; private set; }

        [DependsOn(nameof(Comments), nameof(Snapshot))]
        [NotNull]
        public ResourceTableValues<ICollection<string>> CommentAnnotations { get; private set; }

        [NotNull]
        public ICollection<CultureKey> Languages => _languages.Keys;

        [DependsOn(nameof(Comment))]
        public bool IsInvariant
        {
            get => Comment.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase) >= 0;
            set
            {
                if (value)
                {
                    if (!IsInvariant)
                    {
                        Comment += InvariantKey;
                    }
                }
                else
                {
                    var comment = Comment;
                    int index;

                    while ((index = comment.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        Contract.Assume((index + InvariantKey.Length) <= comment.Length);
                        comment = comment.Remove(index, InvariantKey.Length);
                    }

                    Comment = comment;
                }
            }
        }

        [DependsOn(nameof(Key))]
        public bool IsDuplicateKey => _duplicateKeyExpression.Match(Key).Success;

        public ReadOnlyCollection<CodeReference> CodeReferences { get; internal set; }

        public double Index { get; set; }

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnIndexChanged()
        {
            Container.OnIndexChanged(this);
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary<CultureKey, ResourceData> Snapshot { get; set; }

        public bool CanEdit(CultureKey cultureKey)
        {
            return Container.CanEdit(cultureKey);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(Comment));
            OnPropertyChanged(nameof(Index));
        }

        public bool HasStringFormatParameterMismatches([NotNull] IEnumerable<object> cultures)
        {
            Contract.Requires(cultures != null);

            return HasStringFormatParameterMismatches(cultures.Select(CultureKey.Parse).Select(lang => Values.GetValue(lang)));
        }

        public bool HasSnapshotDifferences([NotNull] IEnumerable<object> cultures)
        {
            Contract.Requires(cultures != null);

            return Snapshot != null && cultures.Select(CultureKey.Parse).Any(IsSnapshotDifferent);
        }

        private bool IsSnapshotDifferent([NotNull] CultureKey culture)
        {
            Contract.Requires(culture != null);

            if (Snapshot == null)
                return false;

            var snapshotValue = Snapshot.GetValueOrDefault(culture)?.Text ?? string.Empty;
            var currentValue = Values.GetValue(culture) ?? string.Empty;

            var snapshotComment = Snapshot.GetValueOrDefault(culture)?.Comment ?? string.Empty;
            var currentComment = Comments.GetValue(culture) ?? string.Empty;

            return !string.Equals(snapshotValue, currentValue) || !string.Equals(snapshotComment, currentComment);
        }

        private void Values_ValueChanged(object sender, EventArgs e)
        {
            OnValuesChanged();
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Input)]
        private void OnValuesChanged()
        {
            OnPropertyChanged(nameof(Values));
        }

        private void Comments_ValueChanged(object sender, EventArgs e)
        {
            OnCommentsChanged();
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Input)]
        private void OnCommentsChanged()
        {
            OnPropertyChanged(nameof(Comments));
        }

        [NotNull]
        private ICollection<string> GetValueAnnotations([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);
            Contract.Ensures(Contract.Result<ICollection<string>>() != null);

            return GetStringFormatParameterMismatchAnnotations(language)
                .Concat(GetSnapshotDifferences(language, Values.GetValue(language.CultureKey), d => d.Text))
                .ToArray();
        }

        [NotNull]
        private ICollection<string> GetCommentAnnotations([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);
            Contract.Ensures(Contract.Result<ICollection<string>>() != null);

            return GetSnapshotDifferences(language, Comments.GetValue(language.CultureKey), d => d.Comment)
                .ToArray();
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetSnapshotDifferences([NotNull] ResourceLanguage language, string current, [NotNull] Func<ResourceData, string> selector)
        {
            Contract.Requires(language != null);
            Contract.Requires(selector != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            var snapshot = Snapshot;
            if (snapshot == null)
                yield break;

            var snapshotValue = snapshot.GetValueOrDefault(language.CultureKey).Maybe().Return(selector) ?? string.Empty;
            if (snapshotValue.Equals(current ?? string.Empty))
                yield break;

            yield return string.Format(CultureInfo.CurrentCulture, Resources.SnapshotAnnotation, snapshotValue);
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetStringFormatParameterMismatchAnnotations([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (language.IsNeutralLanguage)
                yield break;

            var value = language.GetValue(Key);
            if (string.IsNullOrEmpty(value))
                yield break;

            var neutralValue = NeutralLanguage.GetValue(Key);
            if (string.IsNullOrEmpty(neutralValue))
                yield break;

            if (HasStringFormatParameterMismatches(neutralValue, value))
                yield return Resources.StringFormatParameterMismatchError;
        }

        private static bool HasStringFormatParameterMismatches([NotNull] params string[] values)
        {
            Contract.Requires(values != null);

            return HasStringFormatParameterMismatches((IEnumerable<string>)values);
        }

        private static bool HasStringFormatParameterMismatches([NotNull] IEnumerable<string> values)
        {
            Contract.Requires(values != null);

            values = values.Where(value => !string.IsNullOrEmpty(value)).ToArray();

            if (!values.Any())
                return false;

            return values.Select(GetStringFormatFlags)
                .Distinct()
                .Count() > 1;
        }

        [NotNull]
        private static readonly Regex _stringFormatParameterPattern = new Regex(@"\{(\d+)(,\d+)?(:\S+)?\}");

        private static long GetStringFormatFlags(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            return _stringFormatParameterPattern
                .Matches(value)
                .Cast<Match>()
                .Aggregate(0L, (a, match) => a | (1L << int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string IDataErrorInfo.this[string columnName] => columnName != nameof(Key) ? null : _keyValidationError;

        string IDataErrorInfo.Error => null;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_duplicateKeyExpression != null);
            Contract.Invariant(!string.IsNullOrEmpty(Key));
            Contract.Invariant(Values != null);
            Contract.Invariant(Comments != null);
            Contract.Invariant(SnapshotValues != null);
            Contract.Invariant(SnapshotComments != null);
            Contract.Invariant(FileExists != null);
            Contract.Invariant(ValueAnnotations != null);
            Contract.Invariant(CommentAnnotations != null);
            Contract.Invariant(NeutralLanguage != null);
            Contract.Invariant(Container != null);
            Contract.Invariant(_languages != null);
            Contract.Invariant(_stringFormatParameterPattern != null);
        }
    }
}
