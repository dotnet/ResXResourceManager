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

        public bool Apply(string? valuePrefix, string? commentPrefix)
        {
            if (!_entry.CanEdit(TargetCulture))
                return false;

            _entry.Values.SetValue(TargetCulture, valuePrefix + Translation);

            if (commentPrefix == null)
                return true;

            var existingComment = _entry.Comment;
            if (existingComment != null && existingComment.StartsWith(commentPrefix, System.StringComparison.Ordinal))
                return true;

            if (!_entry.CanEdit(_entry.NeutralLanguage.CultureKey))
                return false;

            _entry.Comments.SetValue(_entry.NeutralLanguage.CultureKey, commentPrefix + existingComment);

            return true;
        }

        private static ICollectionView CreateOrderedResults(IList results)
        {
            var orderedResults = new ListCollectionView(results);

            orderedResults.SortDescriptions.Add(new SortDescription(nameof(ITranslationMatch.Rating), ListSortDirection.Descending));
            orderedResults.SortDescriptions.Add(new SortDescription(nameof(ITranslationMatch.Translator) + "." + nameof(ITranslator.DisplayName), ListSortDirection.Ascending));

            return orderedResults;
        }
    }
}