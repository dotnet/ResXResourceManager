namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using tomenglertde.ResXManager.Model.Properties;

    /// <summary>
    /// An indexer that maps the language to the localized string for a resource table entry with the specified resource key; 
    /// the language name is the indexer key and the localized text is the indexer value.
    /// </summary>
    public sealed class ResourceTableValues<T>
    {
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        private readonly Func<ResourceLanguage, T> _getter;
        private readonly Func<ResourceLanguage, T, bool> _setter;

        public ResourceTableValues(IDictionary<CultureKey, ResourceLanguage> languages, Func<ResourceLanguage, T> getter, Func<ResourceLanguage, T, bool> setter)
        {
            Contract.Requires(languages != null);
            Contract.Requires(getter != null);
            Contract.Requires(setter != null);

            _languages = languages;
            _getter = getter;
            _setter = setter;
        }

        public T this[string cultureKey]
        {
            get
            {
                return GetValue(cultureKey.ToCulture());
            }
            set
            {
                SetValue(cultureKey.ToCulture(), value);
            }
        }

        public T GetValue(CultureInfo culture)
        {
            return GetValue(new CultureKey(culture));
        }

        public T GetValue(CultureKey cultureKey)
        {
            Contract.Requires(cultureKey != null);

            ResourceLanguage language;

            if (!_languages.TryGetValue(cultureKey, out language))
                return default(T);

            Contract.Assume(language != null);

            return _getter(language);
        }

        public bool SetValue(CultureInfo culture, T value)
        {
            return SetValue(new CultureKey(culture), value);
        }

        public bool SetValue(CultureKey cultureKey, T value)
        {
            Contract.Requires(cultureKey != null);

            ResourceLanguage language;
            
            if (!_languages.TryGetValue(cultureKey, out language))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.LanguageNotDefinedError, cultureKey.Culture));

            if (!_setter(language, value))
                return false;

            OnValueChanged();
            return true;
        }

        public event EventHandler ValueChanged;

        private void OnValueChanged()
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

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
