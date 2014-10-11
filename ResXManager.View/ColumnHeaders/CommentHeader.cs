namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeaderBase
    {
        public CommentHeader(CultureKey cultureKey)
            : base(cultureKey)
        {
            Contract.Requires(cultureKey != null);
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
