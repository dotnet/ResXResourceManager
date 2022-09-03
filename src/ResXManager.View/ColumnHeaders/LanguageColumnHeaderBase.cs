namespace ResXManager.View.ColumnHeaders
{
    using System.ComponentModel;
    using System.Globalization;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;

    public abstract partial class LanguageColumnHeaderBase : INotifyPropertyChanged, ILanguageColumnHeader
    {
        private readonly IConfiguration _configuration;

        protected LanguageColumnHeaderBase(IConfiguration configuration, CultureKey cultureKey)
        {
            _configuration = configuration;
            CultureKey = cultureKey;
        }

        public CultureKey CultureKey { get; }

        public CultureInfo EffectiveCulture => CultureKey.Culture ?? _configuration.NeutralResourcesLanguage;

        public abstract ColumnType ColumnType { get; }
    }
}