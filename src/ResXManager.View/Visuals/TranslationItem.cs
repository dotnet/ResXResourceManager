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

            // Update the value whether we have a valuePrefix or not so that it gets the new Translation value
            var newValue = valuePrefix != null ? valuePrefix + Translation : Translation;

            var updatedValueSuccessfully = _entry.Values.SetValue(TargetCulture, newValue);

            if (commentPrefix == null)
                return updatedValueSuccessfully;

            // We only need to update the comment if a commentPrefix gets passed in
            bool updatedCommentSuccessfully;

            if (!_entry.CanEdit(_entry.NeutralLanguage.CultureKey))
                return false;

            var existingComment = _entry.Comment ?? string.Empty;
            if (existingComment != null && existingComment.StartsWith(commentPrefix, System.StringComparison.CurrentCulture))
                return true;
            else
                updatedCommentSuccessfully = _entry.Comments.SetValue(_entry.NeutralLanguage.CultureKey, commentPrefix + existingComment);

            if (valuePrefix == null)
                return updatedCommentSuccessfully;
            else
                return updatedValueSuccessfully && updatedCommentSuccessfully;
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