namespace ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using ResXManager.Infrastructure;

    public sealed class TranslationSession : INotifyPropertyChanged, ITranslationSession
    {
        private readonly ObservableCollection<string> _internalMessage = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public TranslationSession(TaskFactory mainThread, CultureInfo? sourceLanguage, CultureInfo neutralResourcesLanguage, ICollection<ITranslationItem> items)
        {
            MainThread = mainThread;
            SourceLanguage = sourceLanguage ?? neutralResourcesLanguage;
            NeutralResourcesLanguage = neutralResourcesLanguage;
            Items = items;

            Messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public TaskFactory MainThread { get; }

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
            try
            {
                await MainThread.StartNew(() => _internalMessage.Add(text), CancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public void Cancel()
        {
            IsCanceled = true;
            _cancellationTokenSource.Cancel();
        }

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        public void Dispose()
        {
            IsComplete = true;
            _cancellationTokenSource.Dispose();
        }
    }
}
