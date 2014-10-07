namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    public abstract class LanguageColumnHeader : ObservableObject, ILanguageColumnHeader
    {
        private readonly CultureKey _cultureKey;

        protected LanguageColumnHeader(CultureKey cultureKey)
        {
            _cultureKey = cultureKey;

            NeutralCultureCountyOverrides.Default.OverrideChanged += NeutralCultureCountyOverrides_OverrideChanged;
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
                return _cultureKey;
            }
        }

        public abstract ColumnType ColumnType
        {
            get;
        }
    }
}
