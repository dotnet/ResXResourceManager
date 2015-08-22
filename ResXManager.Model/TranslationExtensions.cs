namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators;

    static class TranslationExtensions
    {
        public static ICollection<TranslationItem> GetItemsToTranslate(this ResourceManager resourceManager, CultureKey sourceCulture, IEnumerable<CultureKey> targetCultures)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(sourceCulture != null);
            Contract.Requires(targetCultures != null);
            Contract.Ensures(Contract.Result<ICollection<TranslationItem>>() != null);

            return new ObservableCollection<TranslationItem>(
                targetCultures.SelectMany(targetCulture =>
                    resourceManager.ResourceTableEntries
                        .Where(entry => !entry.IsInvariant)
                        .Where(entry => string.IsNullOrWhiteSpace(entry.Values.GetValue(targetCulture)))
                        .Select(entry => new { Entry = entry, Source = entry.Values.GetValue(sourceCulture) })
                        .Where(item => !string.IsNullOrWhiteSpace(item.Source))
                        .Select(item => new TranslationItem(item.Entry, item.Source, targetCulture))));
        }

        public static void ApplyExistingTranslations(this ResourceManager resourceManager, IEnumerable<TranslationItem> items, CultureKey sourceCulture)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(items != null);
            Contract.Requires(sourceCulture != null);

            foreach (var item in items)
            {
                var targetItem = item;
                Contract.Assume(targetItem != null);
                var targetCulture = targetItem.TargetCulture;

                var existingTranslations = resourceManager.ResourceTableEntries
                    .Where(entry => entry != targetItem.Entry)
                    .Where(entry => !entry.IsInvariant)
                    .Where(entry => entry.Values.GetValue(sourceCulture) == targetItem.Source)
                    .Select(entry => entry.Values.GetValue(targetCulture))
                    .Where(translation => !string.IsNullOrWhiteSpace(translation))
                    .GroupBy(translation => translation);

                foreach (var translation in existingTranslations)
                {
                    Contract.Assume(translation != null);
                    item.Results.Add(new TranslationMatch(null, translation.Key, translation.Count()));
                }
            }
        }
    }
}