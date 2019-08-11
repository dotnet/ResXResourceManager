namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf;

    public class TranslationSession : INotifyPropertyChanged, ITranslationSession
    {
        [NotNull]
        [ItemNotNull]
        private readonly ObservableCollection<string> _internalMessage = new ObservableCollection<string>();

        public TranslationSession([CanBeNull] CultureInfo sourceLanguage, [NotNull] CultureInfo neutralResourcesLanguage, [NotNull][ItemNotNull] ICollection<ITranslationItem> items)
        {
            SourceLanguage = sourceLanguage ?? neutralResourcesLanguage;
            NeutralResourcesLanguage = neutralResourcesLanguage;
            Items = items;

            Messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
