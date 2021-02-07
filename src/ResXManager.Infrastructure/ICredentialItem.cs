namespace ResXManager.Infrastructure
{
    using System.ComponentModel;

    public interface ICredentialItem : INotifyPropertyChanged
    {
        string Key { get; }

        string Description { get; }

        string? Value { get; set; }

        bool IsPassword { get; }
    }
}