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
        private readonly CultureInfo _sourceLanguage;
        private readonly CultureInfo _neutralResourcesLanguage;
        private readonly IList<ITranslationItem> _items;

        public Session(CultureInfo sourceLanguage, CultureInfo neutralResourcesLanguage, IList<ITranslationItem> items)
        {
            Contract.Requires(neutralResourcesLanguage != null);
            Contract.Requires(items != null);

            _sourceLanguage = sourceLanguage ?? neutralResourcesLanguage;
            _neutralResourcesLanguage = neutralResourcesLanguage;
            _items = items;

            _messages = new ReadOnlyObservableCollection<string>(_internalMessage);
        }

        public CultureInfo SourceLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return _sourceLanguage;
            }
        }

        public CultureInfo NeutralResourcesLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return _neutralResourcesLanguage;
            }
        }

        public IList<ITranslationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ITranslationItem>>() != null);

                return _items;
            }
        }

        public bool IsCanceled
        {
            get
            {
                return _isCanceled;
            }
            private set
            {
                SetProperty(ref _isCanceled, value, () => IsCanceled);
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
        public bool IsActive => !IsComplete;

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
            Contract.Invariant(_sourceLanguage != null);
            Contract.Invariant(_neutralResourcesLanguage != null);
            Contract.Invariant(_items != null);
        }
    }
}
