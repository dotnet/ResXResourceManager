namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : LanguageColumnHeader
    {
        public LanguageHeader()
            : base(new CultureKey())
        {
        }

        public LanguageHeader(CultureKey cultureKey)
            : base(cultureKey)
        {
            Contract.Requires(cultureKey != null);
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
