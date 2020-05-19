namespace ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf;

    public class LanguageSelectionBoxViewModel : ObservableObject
    {
        public LanguageSelectionBoxViewModel([NotNull][ItemNotNull] IEnumerable<CultureInfo> existingLanguages)
        {
            Languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();
        }

        [Required]
        [CanBeNull]
        public CultureInfo SelectedLanguage { get; set; }

        [ItemNotNull]
        [CanBeNull]
        public ICollection<CultureInfo> Languages { get; }
    }
}
