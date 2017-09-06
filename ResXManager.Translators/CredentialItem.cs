namespace tomenglertde.ResXManager.Translators
{
    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    public class CredentialItem : ObservableObject, ICredentialItem
    {
        public CredentialItem(string key, string description)
        {
            Key = key;
            Description = description;
        }

        public string Key { get; }

        public string Description { get; }

        public string Value { get; set; }
    }
}