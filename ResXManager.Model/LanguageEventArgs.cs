namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    public class LanguageEventArgs : EventArgs
    {
        [NotNull]
        private readonly ResourceLanguage _language;

        public LanguageEventArgs([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);

            _language = language;
        }

        [NotNull]
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
        [NotNull]
        private readonly ResourceEntity _entity;
        private readonly CultureInfo _culture;

        public LanguageChangingEventArgs([NotNull] ResourceEntity entity, CultureInfo culture)
        {
            Contract.Requires(entity != null);

            _entity = entity;
            _culture = culture;
        }

        [NotNull]
        public ResourceEntity Entity
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);
                return _entity;
            }
        }

        public CultureInfo Culture => _culture;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entity != null);
        }
    }
}
