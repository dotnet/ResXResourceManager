namespace tomenglertde.ResXManager.Translators
{
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    public class CredentialItem : ObservableObject, ICredentialItem
    {
        public CredentialItem([NotNull] string key, [NotNull] string description)
        {
            Contract.Requires(key != null);
            Contract.Requires(description != null);

            Key = key;
            Description = description;
        }

        public string Key { get; }

        public string Description { get; }

        public string Value { get; set; }
    }
}