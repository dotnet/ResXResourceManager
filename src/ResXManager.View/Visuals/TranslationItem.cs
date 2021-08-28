namespace ResXManager.View.Visuals
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;

    public class TranslationItem : ObservableObject, ITranslationItem
    {
        private readonly ObservableCollection<ITranslationMatch> _results = new();
        private readonly ResourceTableEntry _entry;

        private ICollectionView? _orderedResults;

        private string? _translation;

        public TranslationItem(ResourceTableEntry entry, string source, CultureKey targetCulture)
        {
            _entry = entry;
            Source = source;

            TargetCulture = targetCulture;
            _results.CollectionChanged += (_, __) => OnPropertyChanged(() => Translation);
        }

        public string Source { get; }

        public CultureKey TargetCulture { get; }

        public IList<ITranslationMatch> Results => _results;

        public ICollectionView OrderedResults => _orderedResults ??= CreateOrderedResults(_results);

        public string? Translation
        {
            get => _translation ?? _results.OrderByDescending(r => r.Rating).Select(r => r.TranslatedText).FirstOrDefault();
            set => _translation = value;
        }

        public bool Apply(string? prefix)
        {
            if (!_entry.CanEdit(TargetCulture))
                return false;

            return _entry.Values.SetValue(TargetCulture, prefix + Translation);
        }

        private static ICollectionView CreateOrderedResults(IList results)
        {
            var orderedResults = new ListCollectionView(results);

            orderedResults.SortDescriptions.Add(new SortDescription("Rating", ListSortDirection.Descending));
            orderedResults.SortDescriptions.Add(new SortDescription("Translator.DisplayName", ListSortDirection.Ascending));

            return orderedResults;
        }
    }
}