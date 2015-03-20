namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();
        private string _translation;
        private ListCollectionView _orderedResults;

        public TranslationItem()
        {
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
            _orderedResults = new ListCollectionView(_results);
            _orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
        }

        public ResourceTableEntry Entry
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

        public ICollectionView OrderedResults
        {
            get
            {
                return _orderedResults;   
            }
        }

        public string Translation
        {
            get
            {
                return _translation ?? _results.OrderByDescending(r => r.Rating).Select(r => r.TranslatedText).FirstOrDefault();
            }
            set
            {
                SetProperty(ref _translation, value, () => Translation);
            }
        }
    }
}