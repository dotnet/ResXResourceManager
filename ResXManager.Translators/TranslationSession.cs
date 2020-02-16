namespace ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    public sealed class TranslationSession : INotifyPropertyChanged, ITranslationSession
    {
        [ItemNotNull]
        private readonly ObservableCollection<string> _internalMessage = new ObservableCollection<string>();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TranslationSession([CanBeNull] CultureInfo sourceLanguage, [NotNull] CultureInfo neutralResourcesLanguage, [NotNull][ItemNotNull] ICollection<ITranslationItem> items)
        {
            SourceLanguage = sourceLanguage ?? neutralResourcesLanguage;
            NeutralResourcesLanguage = neutralResourcesLanguage;
            Items = items;

            Messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public TaskFactory MainThread { get; } = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());

        public CultureInfo SourceLanguage { get; }

        public CultureInfo NeutralResourcesLanguage { get; }

        public ICollection<ITranslationItem> Items { get; }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public bool IsCanceled { get; private set; }

        public int Progress { get; set; }

        public bool IsComplete { get; private set; }

        public bool IsActive => !IsComplete;

        public IList<string> Messages { get; }

        public async void AddMessage(string text)
        {
            await MainThread.StartNew(() => _internalMessage.Add(text), CancellationToken);
        }

        public void Cancel()
        {
            IsCanceled = true;
            _cancellationTokenSource.Cancel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            IsComplete = true;
            _cancellationTokenSource.Dispose();
        }
    }
}
