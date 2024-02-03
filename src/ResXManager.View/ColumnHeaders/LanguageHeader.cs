namespace ResXManager.View.ColumnHeaders;

using System.Globalization;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.View.Properties;

public class LanguageHeader : LanguageColumnHeaderBase
{
    public LanguageHeader(IConfiguration configuration, CultureKey cultureKey)
        : base(configuration, cultureKey)
    {
    }

    public string DisplayName
    {
        get
        {
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