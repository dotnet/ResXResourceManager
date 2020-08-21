namespace ResXManager.Translators
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    public class CredentialItem : ICredentialItem
    {
        public CredentialItem(string key, string description)
        {
            Key = key;
            Description = description;
        }

        public string Key { get; }

        public string Description { get; }

        public string? Value { get; set; }
        
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}