namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    /// <summary>
    /// An indexer that maps the language to the localized string for a resource table entry with the specified resource key; 
    /// the language name is the indexer key and the localized text is the indexer value.
    /// </summary>
    public sealed class ResourceTableValues<T> : IEnumerable<ResourceLanguage>
    {
        [NotNull]
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        [NotNull]
        private readonly Func<ResourceLanguage, T> _getter;
        [NotNull]
        private readonly Func<ResourceLanguage, T, bool> _setter;

        public ResourceTableValues([NotNull] IDictionary<CultureKey, ResourceLanguage> languages, [NotNull] Func<ResourceLanguage, T> getter, [NotNull] Func<ResourceLanguage, T, bool> setter)
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
            get => GetValue(cultureKey);
            set => SetValue(cultureKey, value);
        }

        public T GetValue(object culture)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out ResourceLanguage language))
                return default(T);

            Contract.Assume(language != null);

            return _getter(language);
        }

        public bool SetValue(object culture, T value)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out ResourceLanguage language))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.LanguageNotDefinedError, cultureKey.Culture));

            if (!_setter(language, value))
                return false;

            OnValueChanged();
            return true;
        }

        public bool TrySetValue(object culture, T value)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out ResourceLanguage language))
                return false;

            if (!_setter(language, value))
                return false;

            OnValueChanged();
            return true;
        }

        public event EventHandler ValueChanged;

        public IEnumerator<ResourceLanguage> GetEnumerator()
        {
            return _languages.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_languages != null);
            Contract.Invariant(_getter != null);
            Contract.Invariant(_setter != null);
        }
    }
}
