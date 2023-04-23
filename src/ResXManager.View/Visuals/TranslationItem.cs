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

    public partial class TranslationItem : INotifyPropertyChanged, ITranslationItem
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
            _results.CollectionChanged += (_, __) => OnPropertyChanged(nameof(Translation));
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

        public bool UpdateTranslation(string? prefix = "")
        {
            if (!_entry.CanEdit(TargetCulture))
                return false;

            _entry.Values.SetValue(TargetCulture, prefix + Translation);
            return true;
        }

        public bool UpdateComment(string? prefix, bool useNeutralLanguage, bool useTargetLanguage)
        {
            if (string.IsNullOrEmpty(prefix))
                return true;
            if (!useNeutralLanguage && !useTargetLanguage)
                return true;

            var error = false;
            if (useNeutralLanguage)
            {
                error |= !Apply(_entry.NeutralLanguage.CultureKey);
            }
            if (useTargetLanguage)
            {
                error |= !Apply(TargetCulture);
            }

            return !error;

            bool Apply(CultureKey cultureKey)
            {
                if (!_entry.CanEdit(cultureKey))
                    return false;
                var existingComment = _entry.Comments.GetValue(cultureKey);
                if (existingComment != null && existingComment.StartsWith(prefix, System.StringComparison.Ordinal))
                    return true;
                _entry.Comments.SetValue(cultureKey, prefix + existingComment);
                return true;
            }
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