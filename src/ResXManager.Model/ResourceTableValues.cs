namespace ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    /// <summary>
    /// An indexer that maps the language to the localized string for a resource table entry with the specified resource key; 
    /// the language name is the indexer key and the localized text is the indexer value.
    /// </summary>
    public sealed class ResourceTableValues<T> : IEnumerable<ResourceLanguage>
    {
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        private readonly Func<ResourceLanguage, T> _getter;
        private readonly Func<ResourceLanguage, T, bool> _setter;

        public ResourceTableValues(IDictionary<CultureKey, ResourceLanguage> languages, Func<ResourceLanguage, T> getter, Func<ResourceLanguage, T, bool> setter)
        {
            _languages = languages;
            _getter = getter;
            _setter = setter;
        }

        public T? this[string? cultureKey]
        {
            [return: MaybeNull]
            get => GetValue(cultureKey);
            set => SetValue(cultureKey, value);
        }

        public T? GetValue(object? culture)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out var language))
                return default;

            return _getter(language);
        }

        public bool SetValue(object? culture, T? value)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out var language))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.LanguageNotDefinedError, cultureKey.Culture?.DisplayName ?? Resources.Neutral));

            if (!_setter(language, value!))
                return false;

            OnValueChanged();
            return true;
        }

        public event EventHandler? ValueChanged;

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
    }
}
