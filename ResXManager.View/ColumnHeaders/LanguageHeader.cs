namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : LanguageColumnHeader
    {
        public LanguageHeader()
            : base(null)
        {
        }

        public LanguageHeader(CultureKey cultureKey)
            : base(cultureKey)
        {
        }

        public string DisplayName
        {
            get
            {
                return (CultureKey.Culture != null) ? CultureKey.Culture.DisplayName : Resources.Neutral;
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
