namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Core;

    public class LanguageHeader : LanguageColumnHeaderBase
    {
        public LanguageHeader(Configuration configuration, CultureKey cultureKey)
            : base(configuration, cultureKey)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(cultureKey != null);
        }

        public string DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var cultureInfo = CultureKey.Culture;

                if (cultureInfo == null)
                    return Resources.Neutral;

                return string.Format(CultureInfo.CurrentCulture, "{0} [{1}]", cultureInfo.DisplayName, cultureInfo);
            }
        }

        public override ColumnType ColumnType => ColumnType.Language;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
