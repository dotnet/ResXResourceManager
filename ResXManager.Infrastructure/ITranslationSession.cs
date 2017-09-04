namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    [ContractClass(typeof(TranslationSessionContract))]
    public interface ITranslationSession
    {
        bool IsActive { get; }

        bool IsCanceled { get; }

        bool IsComplete { get; set; }

        [NotNull]
        [ItemNotNull]
        ICollection<ITranslationItem> Items { get; }

        [NotNull]
        IList<string> Messages { get; }

        [NotNull]
        CultureInfo NeutralResourcesLanguage { get; }

        int Progress { get; set; }

        [NotNull]
        CultureInfo SourceLanguage { get; }

        [NotNull]
        Dispatcher Dispatcher { get; }

        void AddMessage([NotNull] string text);

        void Cancel();
    }

    [ContractClassFor(typeof(ITranslationSession))]
    internal abstract class TranslationSessionContract : ITranslationSession
    {
        public abstract bool IsActive { get; }
        public abstract bool IsCanceled { get; }
        public abstract bool IsComplete { get; set; }

        public ICollection<ITranslationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<ITranslationItem>>() != null);
                throw new System.NotImplementedException();
            }
        }

        public IList<string> Messages
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<string>>() != null);
                throw new System.NotImplementedException();
            }
        }

        public CultureInfo NeutralResourcesLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                throw new System.NotImplementedException();
            }
        }

        public abstract int Progress { get; set; }

        public CultureInfo SourceLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                throw new System.NotImplementedException();
            }
        }

        public Dispatcher Dispatcher
        {
            get
            {
                Contract.Ensures(Contract.Result<Dispatcher>() != null);
                throw new System.NotImplementedException();
            }
        }

        public void AddMessage(string text)
        {
            Contract.Requires(text != null);
            throw new System.NotImplementedException();
        }

        public abstract void Cancel();
    }
}