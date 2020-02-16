namespace ResXManager.View.Visuals
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        [NotNull, ItemNotNull]
        private readonly ObservableCollection<ITranslationMatch> _results = new ObservableCollection<ITranslationMatch>();
        [NotNull]
        private readonly ResourceTableEntry _entry;

        [ItemNotNull]
        [CanBeNull]
        private ICollectionView _orderedResults;
        [CanBeNull]
        private string _translation;

        public TranslationItem([NotNull] ResourceTableEntry entry, [NotNull] string source, [NotNull] CultureKey targetCulture)
        {
            _entry = entry;
            Source = source;

            TargetCulture = targetCulture;
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
        }

        [NotNull]
        public string Source { get; }

        [NotNull]
        public CultureKey TargetCulture { get; }

        public IList<ITranslationMatch> Results => _results;

        [NotNull]
        [ItemNotNull]
        public ICollectionView OrderedResults => _orderedResults ?? (_orderedResults = CreateOrderedResults(_results));

        public string Translation
        {
            get => _translation ?? _results.OrderByDescending(r => r.Rating).Select(r => r.TranslatedText).FirstOrDefault();
            set => _translation = value;
        }

        public bool Apply([CanBeNull] string prefix)
        {
            if (!_entry.CanEdit(TargetCulture))
                return false;

            return _entry.Values.SetValue(TargetCulture, prefix + Translation);
        }

        [NotNull]
        [ItemNotNull]
        private static ICollectionView CreateOrderedResults([NotNull][ItemNotNull] IList results)
        {
            var orderedResults = new ListCollectionView(results);

            orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
            orderedResults.SortDescriptions.Add(new SortDescription("Translator.DisplayName", ListSortDirection.Ascending));

            return orderedResults;
        }
    }
}