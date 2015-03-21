namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        private readonly ListCollectionView _orderedResults;
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();

        private string _translation;

        public TranslationItem(ResourceTableEntry entry, string source)
        {
            Contract.Requires(entry != null);
            Contract.Requires(source != null);

            Entry = entry;
            Source = source;

            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
            _orderedResults = new ListCollectionView(_results);
            _orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
            _orderedResults.SortDescriptions.Add(new SortDescription("Translator.DisplayName", ListSortDirection.Ascending));
        }

        public ResourceTableEntry Entry
        {
            get;
            private set;
        }

        public string Source
        {
            get;
            private set;
        }

        public IList<ITranslationMatch> Results
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ITranslationMatch>>() != null);

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

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_orderedResults != null);
            Contract.Invariant(_results != null);
            Contract.Invariant(Entry != null);
            Contract.Invariant(Source != null);
        }
    }
}