namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Globalization;
    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : LanguageColumnHeader
    {
        public LanguageHeader()
            : base(null)
        {
        }

        public LanguageHeader(CultureInfo culture)
            : base(culture)
        {
        }

        public string DisplayName
        {
            get
            {
                return (Language != null) ? Language.DisplayName : Resources.Neutral;
            }
        }

        public override ColumnType ColumnType
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
