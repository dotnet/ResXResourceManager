namespace ResXManager.Model
{
    using JetBrains.Annotations;

    public class ResourceNode
    {
        public ResourceNode([NotNull] string key, string? text, string? comment)
        {
            Text = text;
            Comment = comment;
            Key = key;
        }

        [NotNull]
        public string Key { get;  }
        public string? Text { get;  }
        public string? Comment { get; }
    }
}