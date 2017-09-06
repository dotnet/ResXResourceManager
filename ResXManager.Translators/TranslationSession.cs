namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    public class TranslationSession : ObservableObject, ITranslationSession
    {
        [NotNull]
        private readonly ObservableCollection<string> _internalMessage = new ObservableCollection<string>();

        public TranslationSession(CultureInfo sourceLanguage, [NotNull] CultureInfo neutralResourcesLanguage, [NotNull] ICollection<ITranslationItem> items)
        {
            Contract.Requires(neutralResourcesLanguage != null);
            Contract.Requires(items != null);

            SourceLanguage = sourceLanguage ?? neutralResourcesLanguage;
            NeutralResourcesLanguage = neutralResourcesLanguage;
            Items = items;

            Messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public CultureInfo SourceLanguage { get; }

        public CultureInfo NeutralResourcesLanguage { get; }

        public ICollection<ITranslationItem> Items { get; }

        public bool IsCanceled { get; private set; }

        public int Progress { get; set; }

        public bool IsComplete { get; set; }

        public bool IsActive => !IsComplete;

        public IList<string> Messages { get; }

        public void AddMessage(string text)
        {
            Dispatcher.BeginInvoke(() => _internalMessage.Add(text));
        }

        public void Cancel()
        {
            IsCanceled = true;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_internalMessage != null);
        }
    }
}
