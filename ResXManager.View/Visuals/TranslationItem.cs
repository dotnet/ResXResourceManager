namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Data;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        private readonly CultureKey _targetCulture;
        private readonly ListCollectionView _orderedResults;
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();

        private string _translation;
        private readonly ResourceTableEntry _entry;
        private readonly string _source;

        public TranslationItem(ResourceTableEntry entry, string source, CultureKey targetCulture)
        {
            Contract.Requires(entry != null);
            Contract.Requires(source != null);
            Contract.Requires(targetCulture != null);

            _entry = entry;
            _source = source;

            _targetCulture = targetCulture;
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
            _orderedResults = new ListCollectionView(_results);
            _orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
            _orderedResults.SortDescriptions.Add(new SortDescription("Translator.DisplayName", ListSortDirection.Ascending));
        }

        public ResourceTableEntry Entry
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableEntry>() != null);

                return _entry;
            }
        }

        public string Source
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                return _source;
            }
        }

        public CultureKey TargetCulture => _targetCulture;

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
                Contract.Ensures(Contract.Result<ICollectionView>() != null);

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
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_targetCulture != null);
            Contract.Invariant(_orderedResults != null);
            Contract.Invariant(_results != null);
            Contract.Invariant(_entry != null);
            Contract.Invariant(_source != null);
        }
    }
}