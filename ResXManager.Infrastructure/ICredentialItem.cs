namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof(CredentialItemContract))]
    public interface ICredentialItem : INotifyPropertyChanged
    {
        [NotNull]
        string Key { get; }

        [NotNull]
        string Description { get; }

        [CanBeNull]
        string Value { get; set; }
    }

    [ContractClassFor(typeof(ICredentialItem))]
    internal abstract class CredentialItemContract : ICredentialItem
    {
        public string Key
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Value
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public abstract event PropertyChangedEventHandler PropertyChanged;
    }
}