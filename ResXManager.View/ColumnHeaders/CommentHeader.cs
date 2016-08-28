namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeaderBase
    {
        public CommentHeader(ResourceManager resourceManager, CultureKey cultureKey)
            : base(resourceManager, cultureKey)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(cultureKey != null);
        }

        public override ColumnType ColumnType => ColumnType.Comment;

        public override string ToString()
        {
            return Resources.Comment;
        }
    }
}
