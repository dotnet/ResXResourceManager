namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Globalization;
    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeader
    {
        public CommentHeader()
            : base(null)
        {
        }

        public CommentHeader(CultureInfo culture)
            : base(culture)
        {
        }

        public override ColumnType ColumnType
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
