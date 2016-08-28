namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;
    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;

    [Export]
    public sealed class Translations : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private readonly Configuration _configuration;
        private readonly TranslatorHost _translatorHost;
        private readonly ObservableCollection<TranslationItem> _selectedItems = new ObservableCollection<TranslationItem>();
        private readonly ObservableCollection<CultureKey> _selectedTargetCultures = new ObservableCollection<CultureKey>();

        private CultureKey _sourceCulture;
        private ICollection<TranslationItem> _items = new TranslationItem[0];
        private Session _session;
        private ICollection<CultureKey> _allTargetCultures = new CultureKey[0];


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

            _selectedTargetCultures.CollectionChanged += SelectedTargetCultures_CollectionChanged;
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
                    AllTargetCultures = _resourceManager
                        .CultureKeys
                        .ObservableWhere(key => key != _sourceCulture);
                }
            }
        }

        public ICollection<CultureKey> AllTargetCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<CultureKey>>() != null);

                return _allTargetCultures;
            }
            private set
            {
                Contract.Requires(value != null);

                if (SetProperty(ref _allTargetCultures, value, nameof(AllTargetCultures)))
                {
                    _selectedTargetCultures.SynchronizeWith(value.Except(UnselectedTargetCultures).ToArray());
                }
            }
        }

        public ICollection<CultureKey> SelectedTargetCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<CultureKey>>() != null);

                return _selectedTargetCultures;
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
                Contract.Ensures(Contract.Result<ICollection<TranslationItem>>() != null);

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

        public ICommand StartCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => _session == null, UpdateTargetList);
            }
        }

        public ICommand RestartCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => SourceCulture != null, UpdateTargetList);
            }
        }

        public ICommand ApplyAllCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => IsSessionComplete && Items.Any(), () => Apply(Items));
            }
        }

        public ICommand ApplySelectedCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => IsSessionComplete && SelectedItems.Any(), () => Apply(SelectedItems));
            }
        }

        public ICommand StopCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => IsSessionRunning, Stop);
            }
        }

        private void ResourceManager_Loaded(object sender, EventArgs e)
        {
            if ((SourceCulture == null) || !_resourceManager.CultureKeys.Contains(SourceCulture))
                SourceCulture = _resourceManager.CultureKeys.FirstOrDefault();

            Items = new TranslationItem[0];
        }

        private void Stop()
        {
            _session?.Cancel();
        }

        private void Apply(IEnumerable<TranslationItem> items)
        {
            Contract.Requires(items != null);

            var prefix = _configuration.EffectiveTranslationPrefix;

            foreach (var item in items.Where(item => !string.IsNullOrEmpty(item.Translation)).ToArray())
            {
                Contract.Assume(item != null);

                var entry = item.Entry;

                if (!entry.CanEdit(item.TargetCulture))
                    break;

                entry.Values.SetValue(item.TargetCulture, prefix + item.Translation);
                Items.Remove(item);
            }
        }

        private bool IsSessionComplete => _session != null && _session.IsComplete;

        private bool IsSessionRunning => _session != null && !_session.IsComplete && !_session.IsCanceled;

        private static IEnumerable<CultureKey> UnselectedTargetCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureKey>>() != null);

                return (Settings.Default.TranslationUnselectedTargetCultures ?? string.Empty).Split(',').Select(c => c.ToCultureKey()).Where(c => c != null);
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.TranslationUnselectedTargetCultures = string.Join(",", value);
            }
        }

        private void UpdateTargetList()
        {
            Session?.Cancel();

            SelectedItems.Clear();

            if (_sourceCulture == null)
            {
                Items = new TranslationItem[0];
                return;
            }

            Items = _resourceManager.GetItemsToTranslate(_sourceCulture, _configuration.EffectiveTranslationPrefix, _selectedTargetCultures);

            _resourceManager.ApplyExistingTranslations(Items, _sourceCulture);

            Session = new Session(_sourceCulture.Culture, _configuration.NeutralResourcesLanguage, Items.Cast<ITranslationItem>().ToArray());

            _translatorHost.Translate(Session);
        }

        private void SelectedTargetCultures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UnselectedTargetCultures = _allTargetCultures.Concat(UnselectedTargetCultures).Distinct().Except(_selectedTargetCultures);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_translatorHost != null);
            Contract.Invariant(_selectedItems != null);
            Contract.Invariant(_selectedTargetCultures != null);
            Contract.Invariant(_items != null);
            Contract.Invariant(_allTargetCultures != null);
        }
    }
}
