namespace tomenglertde.ResXManager.Translators
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    public class CredentialItem : INotifyPropertyChanged, ICredentialItem
    {
        public CredentialItem([NotNull] string key, [NotNull] string description)
        {
            Key = key;
            Description = description;
        }

        public string Key { get; }

        public string Description { get; }

        public string Value { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}