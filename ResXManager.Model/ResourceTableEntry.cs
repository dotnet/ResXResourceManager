namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    /// Represents one entry in the resource table.
    /// </summary>
    public class ResourceTableEntry : ObservableObject
    {
        private const string InvariantKey = "@Invariant";
        private readonly ResourceEntity _owner;
        private readonly IDictionary<string, ResourceLanguage> _languages;
        private readonly ResourceLanguage _neutralLanguage;
        private IList<CodeReference> _codeReferences;

        private string _key;
        private ResourceTableValues _values;
        private ResourceTableValues _comments;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTableEntry" /> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="languages">The localized values.</param>
        internal ResourceTableEntry(ResourceEntity owner, string key, IDictionary<string, ResourceLanguage> languages)
        {
            Contract.Requires(owner != null);
            Contract.Requires(!String.IsNullOrEmpty(key));
            Contract.Requires(languages != null);
            Contract.Requires(languages.Any());

            _owner = owner;
            _key = key;
            _languages = languages;

            InitTableValues();

            Contract.Assume(languages.Any());
            _neutralLanguage = languages.First().Value;
            Contract.Assume(_neutralLanguage != null);
            _neutralLanguage.IsNeutralLanguage = true;
        }

        private void InitTableValues()
        {
            if (_values != null)
                _values.ValueChanged -= Values_ValueChanged;

            _values = new ResourceTableValues(_languages, lang => lang.GetValue(_key), (lang, value) => lang.SetValue(_key, value));
            _values.ValueChanged += Values_ValueChanged;

            if (_comments != null)
                _comments.ValueChanged -= Comments_ValueChanged;

            _comments = new ResourceTableValues(_languages, lang => lang.GetComment(_key), (lang, value) => lang.SetComment(_key, value));
            _comments.ValueChanged += Comments_ValueChanged;
        }

        public ResourceEntity Owner
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _owner;
            }
        }

        /// <summary>
        /// Gets the key of the resource.
        /// </summary>
        public string Key
        {
            get
            {
                Contract.Ensures(!String.IsNullOrEmpty(Contract.Result<string>()));
                return _key;
            }
            set
            {
                Contract.Requires(!String.IsNullOrEmpty(value));

                if (_key == value)
                    return;

                var resourceLanguages = _languages.Values;

                if (resourceLanguages.Any(language => language.KeyExists(value)) || !resourceLanguages.All(language => language.CanChange()))
                {
                    Dispatcher.BeginInvoke((Action)(() => OnPropertyChanged("Key")));
                    throw new InvalidOperationException("Key already exists: " + value);
                }

                foreach (var language in resourceLanguages)
                {
                    language.RenameKey(_key, value);
                }

                _key = value;

                InitTableValues();
                OnPropertyChanged("Key");
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
        public ResourceTableValues Values
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues>() != null);
                return _values;
            }
        }

        /// <summary>
        /// Gets the localized comments.
        /// </summary>
        [PropertyDependency("Comment")]
        public ResourceTableValues Comments
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableValues>() != null);
                return _comments;
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

        public IList<CodeReference> CodeReferences
        {
            get
            {
                return _codeReferences;
            }
            internal set
            {
                if (_codeReferences == value)
                    return;

                _codeReferences = value;
                OnPropertyChanged(() => CodeReferences);
            }
        }

        private void Values_ValueChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(() => Values);
        }

        private void Comments_ValueChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(() => Comment);
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

                return x.Owner.Equals(y.Owner) && x.Key.Equals(y.Key);
            }

            public int GetHashCode(ResourceTableEntry obj)
            {
                if (obj == null)
                    throw new ArgumentNullException("obj");

                return obj.Owner.GetHashCode() + obj.Key.GetHashCode();
            }
        }

        public static IEqualityComparer<ResourceTableEntry> EqualityComparer
        {
            get
            {
                return Comparer.Default;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(!String.IsNullOrEmpty(_key));
            Contract.Invariant(_values != null);
            Contract.Invariant(_comments != null);
            Contract.Invariant(_neutralLanguage != null);
            Contract.Invariant(_owner != null);
            Contract.Invariant(_languages != null);
        }
    }
}
