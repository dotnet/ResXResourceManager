namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public class LanguageEventArgs : EventArgs
    {
        private readonly ResourceEntity _entity;
        private readonly ResourceLanguage _language;

        public LanguageEventArgs(ResourceEntity entity, ResourceLanguage language)
        {
            Contract.Requires(entity != null);
            Contract.Requires(language != null);

            _entity = entity;
            _language = language;
        }

        public ResourceEntity Entity
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _entity;
            }
        }

        public ResourceLanguage Language
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceLanguage>() != null);
                return _language;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entity != null);
            Contract.Invariant(_language != null);
        }
    }

    public class LanguageChangedEventArgs : LanguageEventArgs
    {
        public LanguageChangedEventArgs(ResourceEntity entity, ResourceLanguage language)
            : base(entity, language)
        {
            Contract.Requires(entity != null);
            Contract.Requires(language != null);
        }

    }

    public class LanguageChangingEventArgs : CancelEventArgs
    {
        private readonly ResourceEntity _entity;
        private readonly CultureInfo _language;

        public LanguageChangingEventArgs(ResourceEntity entity, CultureInfo language)
        {
            Contract.Requires(entity != null);

            _entity = entity;
            _language = language;
        }

        public ResourceEntity Entity
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _entity;
            }
        }

        public CultureInfo Language
        {
            get
            {
                return _language;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entity != null);
        }
    }
}
