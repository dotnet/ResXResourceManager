namespace tomenglertde.ResXManager.View
{
    using System.Globalization;

    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : ILanguageColumnHeader
    {
        private readonly CultureInfo _culture;

        public LanguageHeader(CultureInfo culture)
        {
            _culture = culture;
        }

        public CultureInfo Language
        {
            get
            {
                return _culture;
            }
        }

        public string DisplayName
        {
            get
            {
                return (_culture != null) ? _culture.DisplayName : Resources.Neutral;
            }
        }

        public ColumnType ColumnType
        {
            get
            {
                return ColumnType.Language;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
