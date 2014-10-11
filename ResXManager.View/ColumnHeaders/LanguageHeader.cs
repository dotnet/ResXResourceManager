namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : LanguageColumnHeaderBase
    {
        public LanguageHeader(CultureKey cultureKey)
            : base(cultureKey)
        {
            Contract.Requires(cultureKey != null);
        }

        public string DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return CultureKey.Culture.Maybe().Return(c => c.DisplayName) ?? Resources.Neutral;
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
