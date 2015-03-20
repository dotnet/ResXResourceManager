namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;

    using TomsToolbox.Desktop;

    public class Session : ObservableObject
    {
        private readonly ObservableCollection<string> _internalMessage = new ObservableCollection<string>();
        private readonly IList<string> _messages;

        private bool _isCancelled;
        private int _progress;
        private bool _isComplete;

        public Session(CultureInfo sourceLanguage, CultureInfo targetLanguage, IList<ITranslationItem> items)
        {
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            Items = items;
            _messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public CultureInfo SourceLanguage
        {
            get;
            private set;
        }

        public CultureInfo TargetLanguage
        {
            get;
            private set;
        }

        public IList<ITranslationItem> Items
        {
            get;
            private set;
        }

        public bool IsCancelled
        {
            get
            {
                return _isCancelled;
            }
            set
            {
                SetProperty(ref _isCancelled,value, () => IsCancelled);
            }
        }

        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                 SetProperty(ref _progress, value, () => Progress);
            }
        }

        public bool IsComplete
        {
            get
            {
                return _isComplete;
            }
            set
            {
                SetProperty(ref _isComplete, value, () => IsComplete);
            }
        }

        public IList<string> Messages
        {
            get
            {
                return _messages;
            }
        }

        public void AddMessage(string text)
        {
            Dispatcher.BeginInvoke(() => _internalMessage.Add(text));
        }
    }
}
