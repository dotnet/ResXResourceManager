namespace ResXManager.View.ColumnHeaders;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.View.Properties;

public class CommentHeader : LanguageColumnHeaderBase
{
    public CommentHeader(IConfiguration configuration, CultureKey cultureKey)
        : base(configuration, cultureKey)
    {
    }

    public override ColumnType ColumnType => ColumnType.Comment;

    public override string ToString()
    {
        return Resources.Comment;
    }
}