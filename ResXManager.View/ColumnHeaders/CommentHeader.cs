namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeader
    {
        public CommentHeader()
            : base(null)
        {
        }

        public CommentHeader(CultureKey cultureKey)
            : base(cultureKey)
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
