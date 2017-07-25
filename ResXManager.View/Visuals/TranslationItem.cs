namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        [NotNull]
        private readonly CultureKey _targetCulture;
        [NotNull]
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();
        [NotNull]
        private readonly ResourceTableEntry _entry;
        [NotNull]
        private readonly string _source;

        private ICollectionView _orderedResults;
        private string _translation;

        public TranslationItem([NotNull] ResourceTableEntry entry, [NotNull] string source, [NotNull] CultureKey targetCulture)
        {
            Contract.Requires(entry != null);
            Contract.Requires(source != null);
            Contract.Requires(targetCulture != null);

            _entry = entry;
            _source = source;

            _targetCulture = targetCulture;
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
        }

        public string Source => _source;

        public CultureKey TargetCulture => _targetCulture;

        public IList<ITranslationMatch> Results => _results;

        [NotNull]
        public ICollectionView OrderedResults
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollectionView>() != null);

                return _orderedResults ?? (_orderedResults = CreateOrderedResults(_results));
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

        public bool Apply(string prefix)
        {
            if (!_entry.CanEdit(_targetCulture))
                return false;

            return _entry.Values.SetValue(_targetCulture, prefix + Translation);
        }

        [NotNull]
        private static ICollectionView CreateOrderedResults([NotNull] IList results)
        {
            Contract.Requires(results != null);
            Contract.Ensures(Contract.Result<ICollectionView>() != null);

            var orderedResults = new ListCollectionView(results);

            orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
            orderedResults.SortDescriptions.Add(new SortDescription("Translator.DisplayName", ListSortDirection.Ascending));

            return orderedResults;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_targetCulture != null);
            Contract.Invariant(_results != null);
            Contract.Invariant(_entry != null);
            Contract.Invariant(_source != null);
        }
    }
}