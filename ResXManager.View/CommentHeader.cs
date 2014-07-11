namespace tomenglertde.ResXManager.View
{
    using System.Globalization;

    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : ILanguageColumnHeader
    {
        private readonly CultureInfo _culture;

        public CommentHeader(CultureInfo culture)
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

        public ColumnType ColumnType
        {
            get
            {
                return ColumnType.Comment;
            }
        }

        public override string ToString()
        {
            return Resources.Comment;
        }
    }
}
