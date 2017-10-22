namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using PropertyChanged;

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