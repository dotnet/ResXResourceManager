namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    [Export]
    public class Translations : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private readonly Configuration _configuration;
        private readonly TranslatorHost _translatorHost;
        private readonly ObservableCollection<TranslationItem> _selectedItems = new ObservableCollection<TranslationItem>();

        private CultureKey _sourceCulture;
        private CultureKey _targetCulture;
        private ICollection<TranslationItem> _items = new TranslationItem[0];
        private Session _session;

        [ImportingConstructor]
        private Translations(ResourceManager resourceManager, Configuration configuration, TranslatorHost translatorHost)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(translatorHost != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
            _translatorHost = translatorHost;
            _resourceManager.Loaded += ResourceManager_Loaded;

            SourceCulture = _resourceManager.CultureKeys.FirstOrDefault();
        }

        void ResourceManager_Loaded(object sender, EventArgs e)
        {
            if ((SourceCulture == null) || !_resourceManager.CultureKeys.Contains(SourceCulture))
                SourceCulture = _resourceManager.CultureKeys.FirstOrDefault();
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

        private void Apply(IEnumerable<TranslationItem> items)
        {
            Contract.Requires(items != null);
            Contract.Assume(_targetCulture != null);

            var prefix = _configuration.PrefixTranslations ? _configuration.TranslationPrefix : string.Empty;

            foreach (var item in items.ToArray())
            {
                Contract.Assume(item != null);

                var entry = item.Entry;

                if (!entry.CanEdit(_targetCulture.Culture))
                    break;

                entry.Values.SetValue(_targetCulture, prefix + item.Translation);
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
            if (Session != null)
                Session.Cancel();

            SelectedItems.Clear();

            if ((_sourceCulture == null) || (_targetCulture == null))
            {
                Items = new TranslationItem[0];
                return;
            }

            GetItemsToTranslate();

            ApplyExistingTranslations();

            var sourceCulture = _sourceCulture.Culture ?? _configuration.NeutralResourcesLanguage;
            var targetCulture = _targetCulture.Culture ?? _configuration.NeutralResourcesLanguage;

            Session = new Session(sourceCulture, targetCulture, Items.Cast<ITranslationItem>().ToArray());

            _translatorHost.Translate(Session);
        }

        private void GetItemsToTranslate()
        {
            Contract.Requires(_sourceCulture != null);
            Contract.Requires(_targetCulture != null);

            Items = new ObservableCollection<TranslationItem>(_resourceManager.ResourceTableEntries
                .Where(entry => !entry.IsInvariant)
                .Where(entry => string.IsNullOrWhiteSpace(entry.Values.GetValue(_targetCulture)))
                .Select(entry => new {Entry = entry, Source = entry.Values.GetValue(_sourceCulture)})
                .Where(item => !string.IsNullOrWhiteSpace(item.Source))
                .Select(item => new TranslationItem(item.Entry, item.Source)));
        }

        private void ApplyExistingTranslations()
        {
            Contract.Requires(_sourceCulture != null);
            Contract.Requires(_targetCulture != null);

            foreach (var item in Items)
            {
                var targetItem = item;
                Contract.Assume(targetItem != null);

                var existingTranslations = _resourceManager.ResourceTableEntries
                    .Where(entry => entry != targetItem.Entry)
                    .Where(entry => !entry.IsInvariant)
                    .Where(entry => entry.Values.GetValue(_sourceCulture) == targetItem.Source)
                    .Select(entry => entry.Values.GetValue(_targetCulture))
                    .Where(translation => !string.IsNullOrWhiteSpace(translation))
                    .GroupBy(translation => translation);

                foreach (var translation in existingTranslations)
                {
                    Contract.Assume(translation != null);
                    item.Results.Add(new TranslationMatch(null, translation.Key, translation.Count()));
                }
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_selectedItems != null);
            Contract.Invariant(_items != null);
        }
    }
}
