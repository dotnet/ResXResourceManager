namespace ResXManager.View.ColumnHeaders
{
    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeaderBase
    {
        public CommentHeader([NotNull] Configuration configuration, [NotNull] CultureKey cultureKey)
            : base(configuration, cultureKey)
        {
        }

        public override ColumnType ColumnType => ColumnType.Comment;

        public override string ToString()
        {
            return Resources.Comment;
        }
    }
}
