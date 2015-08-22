namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
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
                    AllTargetCultures = _resourceManager.CultureKeys.Where(key => key != _sourceCulture).ToArray();
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
            set
            {
                Contract.Requires(value != null);

                if (SetProperty(ref _allTargetCultures, value, () => AllTargetCultures))
                {
                    _selectedTargetCultures.SynchronizeWith(value);
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

                return new DelegateCommand(() => (_session == null), UpdateTargetList);
            }
        }

        public ICommand RestartCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => (SourceCulture != null), UpdateTargetList);
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
            if (_session != null)
                _session.Cancel();
        }

        private void Apply(IEnumerable<TranslationItem> items)
        {
            Contract.Requires(items != null);

            var prefix = _configuration.PrefixTranslations ? _configuration.TranslationPrefix : string.Empty;

            foreach (var item in items.ToArray())
            {
                Contract.Assume(item != null);

                var entry = item.Entry;

                if (!entry.CanEdit(item.TargetCulture.Culture))
                    break;

                entry.Values.SetValue(item.TargetCulture, prefix + item.Translation);
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

            if (_sourceCulture == null)
            {
                Items = new TranslationItem[0];
                return;
            }

            Items = _resourceManager.GetItemsToTranslate(_sourceCulture, _selectedTargetCultures);

            _resourceManager.ApplyExistingTranslations(Items, _sourceCulture);

            Session = new Session(_sourceCulture.Culture, _configuration.NeutralResourcesLanguage, Items.Cast<ITranslationItem>().ToArray());

            _translatorHost.Translate(Session);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_selectedItems != null);
            Contract.Invariant(_selectedTargetCultures != null);
            Contract.Invariant(_items != null);
            Contract.Invariant(_allTargetCultures != null);
        }
    }
}
