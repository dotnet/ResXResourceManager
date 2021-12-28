namespace ResXManager.Model
{
    public class ResourceNode
    {
        public ResourceNode(string key, string? text, string? comment)
        {
            Text = text;
            Comment = comment;
            Key = key;
        }

        public string Key { get; }
        public string? Text { get; }
        public string? Comment { get; }
    }
}