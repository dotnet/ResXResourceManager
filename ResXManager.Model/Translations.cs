namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    public class Translations : ObservableObject
    {
        private readonly ResourceManager _owner;
        private readonly ObservableCollection<TranslationItem> _selectedItems = new ObservableCollection<TranslationItem>();

        private CultureKey _sourceCulture;
        private CultureKey _targetCulture;
        private ICollection<TranslationItem> _items = new TranslationItem[0];
        private Session _session;

        public Translations(ResourceManager owner)
        {
            Contract.Requires(owner != null);

            _owner = owner;
            _owner.Loaded += Owner_Loaded;
        }

        void Owner_Loaded(object sender, System.EventArgs e)
        {
            if (SourceCulture == null)
                SourceCulture = _owner.CultureKeys.FirstOrDefault();
        }

        public CultureKey SourceCulture
        {
            get
            {
                return _sourceCulture;
            }
            set
            {
                if (SetProperty(ref _sourceCulture, value, () => SourceCulture))
                {
                    UpdateTargetList();
                }
            }
        }

        public CultureKey TargetCulture
        {
            get
            {
                return _targetCulture;
            }
            set
            {
                if (SetProperty(ref _targetCulture, value, () => TargetCulture))
                {
                    UpdateTargetList();
                }
            }
        }

        public ICollection<TranslationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<TranslationItem>>() != null);
                return _items;
            }
            private set
            {
                Contract.Requires(value != null);
                SetProperty(ref _items, value, () => Items);
            }
        }

        public ICollection<TranslationItem> SelectedItems
        {
            get
            {
                return _selectedItems;
            }
        }

        public Session Session
        {
            get
            {
                return _session;
            }
            set
            {
                SetProperty(ref _session, value, () => Session);
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return new DelegateCommand(() => (SourceCulture != null) && (TargetCulture != null), UpdateTargetList);
            }
        }

        public ICommand ApplyAllCommand
        {
            get
            {
                return new DelegateCommand(() => IsSessionComplete && Items.Any(), () => Apply(Items));
            }
        }

        public ICommand ApplySelectedCommand
        {
            get
            {
                return new DelegateCommand(() => IsSessionComplete && SelectedItems.Any(), () => Apply(SelectedItems));
            }
        }

        public ICommand StopCommand
        {
            get
            {
                return new DelegateCommand(() => IsSessionRunning, Stop);
            }
        }

        private void Stop()
        {
            if (_session != null)
                _session.Cancel();
        }

        private void Apply(ICollection<TranslationItem> items)
        {
            Contract.Requires(items != null);
            Contract.Assume(_targetCulture != null);

            foreach (var item in items.ToArray())
            {
                Contract.Assume(item != null);

                item.Entry.Values.SetValue(_targetCulture, item.Translation);
                Items.Remove(item);
            }
        }

        private bool IsSessionComplete
        {
            get
            {
                return _session != null && _session.IsComplete;
            }
        }

        private bool IsSessionRunning
        {
            get
            {
                return _session != null && !_session.IsComplete && !_session.IsCanceled;
            }
        }

        private void UpdateTargetList()
        {
            SelectedItems.Clear();

            if ((_sourceCulture == null) || (_targetCulture == null))
            {
                Items = new TranslationItem[0];
                return;
            }

            Items = new ObservableCollection<TranslationItem>(_owner.ResourceTableEntries
                .Where(entry => string.IsNullOrWhiteSpace(entry.Values[_targetCulture.ToString()]))
                .Select(entry => new TranslationItem(entry, entry.Values.GetValue(_sourceCulture)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Source)));

            var sourceCulture = _sourceCulture.Culture ?? _owner.Configuration.NeutralResourcesLanguage;
            var targetCulture = _targetCulture.Culture ?? _owner.Configuration.NeutralResourcesLanguage;

            if (Session != null)
                Session.Cancel();

            Session = new Session(sourceCulture, targetCulture, Items.Cast<ITranslationItem>().ToArray());

            TranslatorHost.Translate(Session);
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_owner != null);
            Contract.Invariant(_selectedItems != null);
            Contract.Invariant(_items != null);
        }
    }
}
