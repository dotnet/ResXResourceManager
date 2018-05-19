namespace tomenglertde.ResXManager.Infrastructure
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    public interface ICredentialItem : INotifyPropertyChanged
    {
        [NotNull]
        string Key { get; }

        [NotNull]
        string Description { get; }

        [CanBeNull]
        string Value { get; set; }
    }
}