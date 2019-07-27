namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Globalization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Wpf;

    public abstract class LanguageColumnHeaderBase : ObservableObject, ILanguageColumnHeader
    {
        [NotNull]
        private readonly Configuration _configuration;

        protected LanguageColumnHeaderBase([NotNull] Configuration configuration, [NotNull] CultureKey cultureKey)
        {
            _configuration = configuration;
            CultureKey = cultureKey;
        }

        public CultureKey CultureKey { get; }

        public CultureInfo EffectiveCulture => CultureKey.Culture ?? _configuration.NeutralResourcesLanguage;

        public abstract ColumnType ColumnType { get; }
    }
}