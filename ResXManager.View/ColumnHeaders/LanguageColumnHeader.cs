namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Globalization;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    public abstract class LanguageColumnHeader : ObservableObject, ILanguageColumnHeader
    {
        private readonly CultureInfo _culture;

        protected LanguageColumnHeader(CultureInfo culture)
        {
            _culture = culture;

            NeutralCultureCountyOverrides.Default.OverrideChanged += NeutralCultureCountyOverrides_OverrideChanged;
        }

        void NeutralCultureCountyOverrides_OverrideChanged(object sender, CultureOverrideEventArgs e)
        {
            if (e.NeutralCulture.Equals(_culture))
            {
                OnPropertyChanged(() => Language);
            }
        }

        public CultureInfo Language
        {
            get
            {
                return _culture;
            }
        }

        public abstract ColumnType ColumnType
        {
            get;
        }
    }
}
