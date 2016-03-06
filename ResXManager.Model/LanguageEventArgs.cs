namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public class LanguageEventArgs : EventArgs
    {
        private readonly ResourceLanguage _language;

        public LanguageEventArgs(ResourceLanguage language)
        {
            Contract.Requires(language != null);

            _language = language;
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
            Contract.Invariant(_language != null);
        }
    }

    public class LanguageChangingEventArgs : CancelEventArgs
    {
        private readonly ResourceEntity _entity;
        private readonly CultureInfo _culture;

        public LanguageChangingEventArgs(ResourceEntity entity, CultureInfo culture)
        {
            Contract.Requires(entity != null);

            _entity = entity;
            _culture = culture;
        }

        public ResourceEntity Entity
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _entity;
            }
        }

        public CultureInfo Culture
        {
            get
            {
                return _culture;
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
