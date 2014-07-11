namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using tomenglertde.ResXManager.Model.Properties;

    /// <summary>
    /// A dictionary that maps the language to the localized string for a resource table entry with the specified resource key; 
    /// the language name is the dictionary key and the localized text is the dictionary value.
    /// </summary>
    public sealed class ResourceTableValues : IDictionary<string, string>
    {
        private readonly IDictionary<string, ResourceLanguage> _languages;
        private readonly Func<ResourceLanguage, string> _getter;
        private readonly Func<ResourceLanguage, string, bool> _setter;

        public ResourceTableValues(IDictionary<string, ResourceLanguage> languages, Func<ResourceLanguage, string> getter, Func<ResourceLanguage, string, bool> setter)
        {
            Contract.Requires(languages != null);
            Contract.Requires(getter != null);
            Contract.Requires(setter != null);

            _languages = languages;
            _getter = getter;
            _setter = setter;
        }

        public string this[string key]
        {
            get
            {
                ResourceLanguage language;
                if (!_languages.TryGetValue(key, out language))
                    return null;

                Contract.Assume(language != null);

                return _getter(language);
            }
            set
            {
                Contract.Assume(key != null);
                SetValue(key, value);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Every indexer throws IndexOutOfRange")]
        public bool SetValue(string languageName, string value)
        {
            Contract.Requires(languageName != null);

            var language = _languages[languageName];
            if (language == null)
                throw new IndexOutOfRangeException(string.Format(CultureInfo.CurrentCulture, Resources.LanguageNotDefinedError, languageName));

            if (_setter(language, value))
            {
                OnValueChanged();
                return true;
            }

            return false;
        }

        public event EventHandler ValueChanged;

        private void OnValueChanged()
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        #region IDictionary<string,string> Members

        void IDictionary<string, string>.Add(string key, string value)
        {
            throw new NotImplementedException();
        }

        [ContractVerification(false)]
        bool IDictionary<string, string>.ContainsKey(string key)
        {
            return _languages.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get
            {
                return _languages.Keys;
            }
        }

        bool IDictionary<string, string>.Remove(string key)
        {
            throw new NotImplementedException();
        }

        [ContractVerification(false)]
        public bool TryGetValue(string key, out string value)
        {
            ResourceLanguage language;

            if (!_languages.TryGetValue(key, out language))
            {
                value = null;
                return false;
            }

            Contract.Assume(language != null);

            return (value = _getter(language)) != null;
        }

        public ICollection<string> Values
        {
            get
            {
                return _languages.Values.Select(language => _getter(language)).ToArray();
            }
        }

        #endregion

        #region ICollection<KeyValuePair<string,string>> Members

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, string>>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return _languages.Count;
            }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _languages.Values.Select(language => new KeyValuePair<string, string>(language.Name, _getter(language))).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_languages != null);
            Contract.Invariant(_getter != null);
            Contract.Invariant(_setter != null);
        }
    }
}
