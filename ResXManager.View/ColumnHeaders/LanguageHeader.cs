namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Globalization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class LanguageHeader : LanguageColumnHeaderBase
    {
        public LanguageHeader([NotNull] Configuration configuration, [NotNull] CultureKey cultureKey)
            : base(configuration, cultureKey)
        {
        }

        [NotNull]
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
}
