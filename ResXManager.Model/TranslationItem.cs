namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();
        private string _translation;

        public TranslationItem()
        {
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
        }

        public ResourceTableEntry Entry
        {
            get;
            set;
        }

        public bool Apply
        {
            get;
            set;
        }

        public string Source
        {
            get;
            set;
        }

        public IList<ITranslationMatch> Results
        {
            get
            {
                return _results;
            }
        }

        public string Translation
        {
            get
            {
                return _translation ?? _results.OrderBy(r => r.Rating).Select(r => r.TranslatedText).FirstOrDefault();
            }
            set
            {
                SetProperty(ref _translation, value, () => Translation);
            }
        }
    }
}