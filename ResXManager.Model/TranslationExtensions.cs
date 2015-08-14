namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using tomenglertde.ResXManager.Translators;

    static class TranslationExtensions
    {
        public static ICollection<TranslationItem> GetItemsToTranslate(this ResourceManager resourceManager, CultureKey sourceCulture, CultureKey targetCulture)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(sourceCulture != null);
            Contract.Requires(targetCulture != null);
            Contract.Ensures(Contract.Result<ICollection<TranslationItem>>() != null);

            return new ObservableCollection<TranslationItem>(resourceManager.ResourceTableEntries
                .Where(entry => !entry.IsInvariant)
                .Where(entry => String.IsNullOrWhiteSpace(entry.Values.GetValue(targetCulture)))
                .Select(entry => new { Entry = entry, Source = entry.Values.GetValue(sourceCulture) })
                .Where(item => !String.IsNullOrWhiteSpace(item.Source))
                .Select(item => new TranslationItem(item.Entry, item.Source)));
        }

        public static void ApplyExistingTranslations(this ResourceManager resourceManager, IEnumerable<TranslationItem> items, CultureKey sourceCulture, CultureKey targetCulture)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(items != null);
            Contract.Requires(sourceCulture != null);
            Contract.Requires(targetCulture != null);

            foreach (var item in items)
            {
                var targetItem = item;
                Contract.Assume(targetItem != null);

                var existingTranslations = resourceManager.ResourceTableEntries
                    .Where(entry => entry != targetItem.Entry)
                    .Where(entry => !entry.IsInvariant)
                    .Where(entry => entry.Values.GetValue(sourceCulture) == targetItem.Source)
                    .Select(entry => entry.Values.GetValue(targetCulture))
                    .Where(translation => !String.IsNullOrWhiteSpace(translation))
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