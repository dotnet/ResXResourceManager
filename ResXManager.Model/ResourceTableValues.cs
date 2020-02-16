namespace ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

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
            _languages = languages;
            _getter = getter;
            _setter = setter;
        }

        [CanBeNull]
        public T this[[CanBeNull] string cultureKey]
        {
            get => GetValue(cultureKey);
            set => SetValue(cultureKey, value);
        }

        [CanBeNull]
        public T GetValue([CanBeNull] object culture)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out var language))
                return default;

            return _getter(language);
        }

        public bool SetValue([CanBeNull] object culture, [CanBeNull] T value)
        {
            var cultureKey = CultureKey.Parse(culture);

            if (!_languages.TryGetValue(cultureKey, out var language))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.LanguageNotDefinedError, cultureKey.Culture?.DisplayName ?? Resources.Neutral));

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
    }
}
