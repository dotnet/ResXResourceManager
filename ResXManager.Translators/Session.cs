namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using TomsToolbox.Desktop;

    public class Session : ObservableObject
    {
        private readonly ObservableCollection<string> _internalMessage = new ObservableCollection<string>();
        private readonly IList<string> _messages;

        private bool _isCanceled;
        private int _progress;
        private bool _isComplete;

        public Session(CultureInfo sourceLanguage, CultureInfo targetLanguage, IList<ITranslationItem> items)
        {
            Contract.Requires(sourceLanguage != null);
            Contract.Requires(targetLanguage != null);
            Contract.Requires(items != null);

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

        public bool IsCanceled
        {
            get
            {
                return _isCanceled;
            }
            private set
            {
                SetProperty(ref _isCanceled,value, () => IsCanceled);
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

        [PropertyDependency("IsComplete")]
        public bool IsActive
        {
            get
            {
                return !IsComplete;
            }
        }

        public IList<string> Messages
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<string>>() != null);
                return _messages;
            }
        }

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
        private void ObjectInvariant()
        {
            Contract.Invariant(_internalMessage != null);
            Contract.Invariant(_messages != null);
            Contract.Invariant(SourceLanguage != null);
            Contract.Invariant(TargetLanguage != null);
            Contract.Invariant(Items != null);
        }
    }
}
