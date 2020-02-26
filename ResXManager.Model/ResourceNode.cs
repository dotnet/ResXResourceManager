namespace ResXManager.Model
{
    using JetBrains.Annotations;

    public class ResourceNode
    {
        public ResourceNode([NotNull] string key, [CanBeNull] string text, [CanBeNull] string comment)
        {
            Text = text;
            Comment = comment;
            Key = key;
        }

        [NotNull]
        public string Key { get;  }
        [CanBeNull]
        public string Text { get;  }
        [CanBeNull]
        public string Comment { get; }
    }
}