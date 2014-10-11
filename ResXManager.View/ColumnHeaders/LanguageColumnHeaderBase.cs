namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Tools;

    public abstract class LanguageColumnHeaderBase : ObservableObject, ILanguageColumnHeader
    {
        private static readonly string _neutralResourceLanguagePropertyName = PropertySupport.ExtractPropertyName(() => Settings.Default.NeutralResourceLanguage);
        private readonly CultureKey _cultureKey;

        protected LanguageColumnHeaderBase(CultureKey cultureKey)
        {
            Contract.Requires(cultureKey != null);

            _cultureKey = cultureKey;

            NeutralCultureCountyOverrides.Default.OverrideChanged += NeutralCultureCountyOverrides_OverrideChanged;
            Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != _neutralResourceLanguagePropertyName)
                return;

            if (_cultureKey.Culture == null)
            {
                OnPropertyChanged(() => CultureKey);
            }
        }

        void NeutralCultureCountyOverrides_OverrideChanged(object sender, CultureOverrideEventArgs e)
        {
            if (e.NeutralCulture.Equals(_cultureKey.Culture))
            {
                OnPropertyChanged(() => CultureKey);
            }
        }

        public CultureKey CultureKey
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureKey>() != null);

                return _cultureKey;
            }
        }

        public abstract ColumnType ColumnType
        {
            get;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_cultureKey != null);
        }
    }
}
