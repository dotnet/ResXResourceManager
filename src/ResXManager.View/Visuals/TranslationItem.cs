namespace ResXManager.View.Visuals
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Essentials;

    public partial class TranslationItem : INotifyPropertyChanged, ITranslationItem
    {
        private readonly ObservableCollection<ITranslationMatch> _results = new();
        private readonly ResourceTableEntry _entry;

        // for tooltip in TranslationsView.xaml
        public ResourceTableEntry Entry => _entry;

        private ICollectionView? _orderedResults;

        private string? _translation;

        public TranslationItem(ResourceTableEntry entry, string source, CultureKey targetCulture)
        {
            _entry = entry;
            Source = source;
            TargetCulture = targetCulture;
            _results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Translation));
        }

        public string Source { get; }

        public CultureKey TargetCulture { get; }

        public IList<(CultureInfo Culture, string Text, string? Comment)> GetAllItems(CultureInfo neutralCulture)
        {
            // ! Text is checked in Where clause
            return _entry.Languages
                .Select(cultureKey => (cultureKey.Culture ?? neutralCulture, Text: _entry.Values.GetValue(cultureKey), Comment: _entry.Comments.GetValue(cultureKey)))
                .Where(item => !item.Text.IsNullOrWhiteSpace())
                .ToList()!;
        }

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