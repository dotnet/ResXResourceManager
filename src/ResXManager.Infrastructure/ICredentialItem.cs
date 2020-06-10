namespace ResXManager.Infrastructure
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    public interface ICredentialItem : INotifyPropertyChanged
    {
        [NotNull]
        string Key { get; }

        [NotNull]
        string Description { get; }

        string? Value { get; set; }
    }
}