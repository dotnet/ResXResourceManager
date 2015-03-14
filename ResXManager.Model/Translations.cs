namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;

    public class Translations : ObservableObject
    {
        private readonly ResourceManager _owner;
        private CultureKey _sourceCulture;
        private CultureKey _targetCulture;
        private ICollection<TranslationItem> _items;

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

        private void UpdateTargetList()
        {
            if ((_sourceCulture == null) || (_targetCulture == null))
            {
                Items = null;
                return;
            }

            Items = _owner.ResourceTableEntries
                .Where(entry => string.IsNullOrWhiteSpace(entry.Values[_targetCulture.ToString()]))
                .Select(entry => new TranslationItem
                {
                    Entry = entry,
                    Source = entry.Values[_sourceCulture.ToString()],
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Source))
                .ToArray();

            var sourceCulture = _sourceCulture.Culture ?? _owner.Configuration.NeutralResourcesLanguage;

            TranslatorHost.Translate(Dispatcher, sourceCulture, _targetCulture.Culture, Items.Cast<ITranslationItem>().ToArray());
        }
    }
}
