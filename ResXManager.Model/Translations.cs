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
        private ICollection<TranslationItem> _items;
        private Session _session;

        public Translations(ResourceManager owner)
        {
            Contract.Requires(owner != null);

            _owner = owner;
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
                return _items;
            }
            set
            {
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

        public ICommand ApplyCommand
        {
            get
            {
                return new DelegateCommand(() => SelectedItems.Any(), Apply);
            }
        }

        private void Apply()
        {
            foreach (var item in SelectedItems.ToArray())
            {
                item.Entry.Values.SetValue(_targetCulture, item.Translation);
                Items.Remove(item);
            }   
        }

        private void UpdateTargetList()
        {
            SelectedItems.Clear();

            if ((_sourceCulture == null) || (_targetCulture == null))
            {
                Items = null;
                return;
            }

            Items = new ObservableCollection<TranslationItem>(_owner.ResourceTableEntries
                .Where(entry => string.IsNullOrWhiteSpace(entry.Values[_targetCulture.ToString()]))
                .Select(entry => new TranslationItem
                {
                    Entry = entry,
                    Source = entry.Values.GetValue(_sourceCulture)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Source)));

            SelectedItems.AddRange(Items);

            var sourceCulture = _sourceCulture.Culture ?? _owner.Configuration.NeutralResourcesLanguage;

            if (Session != null)
                Session.IsCancelled = true;

            Session = new Session(sourceCulture, _targetCulture.Culture, Items.Cast<ITranslationItem>().ToArray());

            TranslatorHost.Translate(Session);
        }
    }
}
