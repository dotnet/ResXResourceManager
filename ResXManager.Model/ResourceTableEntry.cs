namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Represents one entry in the resource table.
    /// </summary>
    public class ResourceTableEntry : ObservableObject
    {
        private const string InvariantKey = "@Invariant";
        private readonly Regex _duplicateKeyExpression = new Regex(@"_Duplicate\[\d+\]$");
        private readonly ResourceEntity _container;

        private string _key;

        private IDictionary<CultureKey, ResourceLanguage> _languages;
        private ResourceTableValues<string> _values;
        private ResourceTableValues<string> _comments;
        private ResourceTableValues<bool> _fileExists;
        private ResourceTableValues<ICollection<string>> _valueAnnotations;
        private ResourceTableValues<ICollection<string>> _commentAnnotations;
        private ResourceLanguage _neutralLanguage;

        private IList<CodeReference> _codeReferences;
        private double _index;
        private IDictionary<CultureKey, ResourceData> _snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTableEntry" /> class.
        /// </summary>
        /// <param name="container">The owner.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="index">The original index of the resource in the file.</param>
        /// <param name="languages">The localized values.</param>
        internal ResourceTableEntry(ResourceEntity container, string key, double index, IDictionary<CultureKey, ResourceLanguage> languages)
        {
            Contract.Requires(container != null);
            Contract.Requires(!string.IsNullOrEmpty(key));
            Contract.Requires(languages != null);
            Contract.Requires(languages.Any());

            _container = container;
            _key = key;
            _index = index;
            _languages = languages;

            _values = new ResourceTableValues<string>(_languages, lang => lang.GetValue(_key), (lang, value) => lang.SetValue(_key, value));
            _values.ValueChanged += Values_ValueChanged;

            _comments = new ResourceTableValues<string>(_languages, lang => lang.GetComment(_key), (lang, value) => lang.SetComment(_key, value));
            _comments.ValueChanged += Comments_ValueChanged;

            _fileExists = new ResourceTableValues<bool>(_languages, lang => true, (lang, value) => false);

            _valueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (lang, value) => false);
            _commentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (lang, value) => false);

            Contract.Assume(languages.Any());
            _neutralLanguage = languages.First().Value;
        }

        private void ResetTableValues()
        {
            _values.ValueChanged -= Values_ValueChanged;
            _values = new ResourceTableValues<string>(_languages, lang => lang.GetValue(_key), (lang, value) => lang.SetValue(_key, value));
            _values.ValueChanged += Values_ValueChanged;

            _comments.ValueChanged -= Comments_ValueChanged;
            _comments = new ResourceTableValues<string>(_languages, lang => lang.GetComment(_key), (lang, value) => lang.SetComment(_key, value));
            _comments.ValueChanged += Comments_ValueChanged;

            _fileExists = new ResourceTableValues<bool>(_languages, lang => true, (lang, value) => false);

            _valueAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetValueAnnotations, (lang, value) => false);
            _commentAnnotations = new ResourceTableValues<ICollection<string>>(_languages, GetCommentAnnotations, (lang, value) => false);
        }

        internal void Update(int index, IDictionary<CultureKey, ResourceLanguage> languages)
        {
            Index = index;

            _languages = languages;

            ResetTableValues();

            Contract.Assume(languages.Any());
            _neutralLanguage = languages.First().Value;

            Refresh();
        }


        public ResourceEntity Container
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _container;
            }
        }

        /// <summary>
        /// Gets the key of the resource.
        /// </summary>
        public string Key
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

                return _key;
            }
            set
            {
                Contract.Requires(!string.IsNullOrEmpty(value));

                if (_key == value)
                    return;

                var resourceLanguages = _languages.Values;

                if (resourceLanguages.Any(language => language.KeyExists(value)) || !resourceLanguages.All(language => language.CanEdit()))
                {
                    Dispatcher.BeginInvoke(() => OnPropertyChanged(() => Key));
                    throw new InvalidOperationException("Key already exists: " + value);
                }

                foreach (var language in resourceLanguages)
                {
                    Contract.Assume(language != null);
                    language.RenameKey(_key, value);
                }

                _key = value;

                ResetTableValues();
                OnPropertyChanged(nameof(Key));
            }
        }

        /// <summary>
        /// Gets or sets the comment of the neutral language.
        /// </summary>
        public string Comment
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _neutralLanguage.GetComment(Key) ?? string.Empty;
            }
            set
            {
                _neutralLanguage.SetComment(Key, value);
                OnPropertyChanged(() => Comment);
            }
        }

        /// <summary>
        /// Gets the localized values.
        /// </summary>
        public ResourceTableValues<string> Values
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues<string>>() != null);
                return _values;
            }
        }

        /// <summary>
        /// Gets the localized comments.
        /// </summary>
        [PropertyDependency("Comment")]
        public ResourceTableValues<string> Comments
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues<string>>() != null);
                return _comments;
            }
        }

        [PropertyDependency("Values")]
        public ResourceTableValues<bool> FileExists
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues<bool>>() != null);
                return _fileExists;
            }
        }

        [PropertyDependency("Values", "Snapshot")]
        public ResourceTableValues<ICollection<string>> ValueAnnotations
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues<ICollection<string>>>() != null);

                return _valueAnnotations;
            }
        }

        [PropertyDependency("Comments", "Snapshot")]
        public ResourceTableValues<ICollection<string>> CommentAnnotations
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues<ICollection<string>>>() != null);

                return _commentAnnotations;
            }
        }

        internal ICollection<CultureKey> Languages
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<CultureKey>>() != null);

                return _languages.Keys;
            }
        }

        [PropertyDependency("Comment")]
        public bool IsInvariant
        {
            get
            {
                return Comment.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase) >= 0;
            }
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

        [PropertyDependency("Key")]
        public bool IsDuplicateKey
        {
            get
            {
                return _duplicateKeyExpression.Match(Key).Success;
            }
        }

        public IList<CodeReference> CodeReferences
        {
            get
            {
                return _codeReferences;
            }
            internal set
            {
                SetProperty(ref _codeReferences, value, () => CodeReferences);
            }
        }

        public double Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (SetProperty(ref _index, value, () => Index))
                {
                    _container.OnIndexChanged(this);
                }
            }
        }

        public static IEqualityComparer<ResourceTableEntry> EqualityComparer
        {
            get
            {
                Contract.Ensures(Contract.Result<IEqualityComparer<ResourceTableEntry>>() != null);

                return Comparer.Default;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary<CultureKey, ResourceData> Snapshot
        {
            get
            {
                return _snapshot;
            }
            set
            {
                SetProperty(ref _snapshot, value, () => Snapshot);
            }
        }

        public bool CanEdit(CultureInfo culture)
        {
            return _container.CanEdit(culture);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(Comment));
        }

        public bool HasStringFormatParameterMismatches(IEnumerable<CultureKey> cultures)
        {
            Contract.Requires(cultures != null);

            return HasStringFormatParameterMismatches(cultures.Select(lang => _values.GetValue(lang)));
        }

        public bool HasSnapshotDifferences(IEnumerable<CultureKey> cultures)
        {
            Contract.Requires(cultures != null);

            return _snapshot != null && cultures.Any(IsSnapshotDifferent);
        }

        private bool IsSnapshotDifferent(CultureKey culture)
        {
            Contract.Requires(culture != null);
            Contract.Requires(_snapshot != null);

            var snapshotValue = _snapshot.GetValueOrDefault(culture).Maybe().Return(x => x.Text) ?? string.Empty;
            var currentValue = _values.GetValue(culture) ?? string.Empty;

            var snapshotComment = _snapshot.GetValueOrDefault(culture).Maybe().Return(x => x.Comment) ?? string.Empty;
            var currentComment = _comments.GetValue(culture) ?? string.Empty;

            return !string.Equals(snapshotValue, currentValue) || !string.Equals(snapshotComment, currentComment);
        }

        private void Values_ValueChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(() => Values);
        }

        private void Comments_ValueChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(() => Comment);
        }

        private ICollection<string> GetValueAnnotations(ResourceLanguage language)
        {
            Contract.Requires(language != null);

            return GetStringFormatParameterMismatchAnnotations(language)
                .Concat(GetSnapshotDifferences(language, Values.GetValue(language.CultureKey), d => d.Text))
                .ToArray();
        }

        private ICollection<string> GetCommentAnnotations(ResourceLanguage language)
        {
            Contract.Requires(language != null);

            return GetSnapshotDifferences(language, Comments.GetValue(language.CultureKey), d => d.Comment)
                .ToArray();
        }

        private IEnumerable<string> GetSnapshotDifferences(ResourceLanguage language, string current, Func<ResourceData, string> selector)
        {
            var snapshot = _snapshot;
            if (snapshot == null)
                yield break;

            var snapshotValue = snapshot.GetValueOrDefault(language.CultureKey).Maybe().Return(selector) ?? string.Empty;
            if (snapshotValue.Equals(current ?? string.Empty))
                yield break;

            yield return string.Format(CultureInfo.CurrentCulture, Resources.SnapshotAnnotation, snapshotValue);
        }

        private IEnumerable<string> GetStringFormatParameterMismatchAnnotations(ResourceLanguage language)
        {
            if (language.IsNeutralLanguage)
                yield break;

            var value = language.GetValue(_key);
            if (string.IsNullOrEmpty(value))
                yield break;

            var neutralValue = _neutralLanguage.GetValue(_key);
            if (string.IsNullOrEmpty(neutralValue))
                yield break;

            if (HasStringFormatParameterMismatches(neutralValue, value))
                yield return Resources.StringFormatParameterMismatchError;
        }

        private static bool HasStringFormatParameterMismatches(params string[] values)
        {
            Contract.Requires(values != null);

            return HasStringFormatParameterMismatches((IEnumerable<string>)values);
        }

        private static bool HasStringFormatParameterMismatches(IEnumerable<string> values)
        {
            Contract.Requires(values != null);

            values = values.Where(value => !string.IsNullOrEmpty(value)).ToArray();

            if (!values.Any())
                return false;

            return values.Select(GetStringFormatFlags)
                .Distinct()
                .Count() > 1;
        }

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

        class Comparer : IEqualityComparer<ResourceTableEntry>
        {
            public static readonly Comparer Default = new Comparer();

            public bool Equals(ResourceTableEntry x, ResourceTableEntry y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null))
                    return false;
                if (ReferenceEquals(y, null))
                    return false;

                return x.Container.Equals(y.Container) && x.Key.Equals(y.Key);
            }

            public int GetHashCode(ResourceTableEntry obj)
            {
                if (obj == null)
                    throw new ArgumentNullException("obj");

                return obj.Container.GetHashCode() + obj.Key.GetHashCode();
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!string.IsNullOrEmpty(_key));
            Contract.Invariant(_values != null);
            Contract.Invariant(_comments != null);
            Contract.Invariant(_fileExists != null);
            Contract.Invariant(_neutralLanguage != null);
            Contract.Invariant(_container != null);
            Contract.Invariant(_languages != null);
        }
    }
}
