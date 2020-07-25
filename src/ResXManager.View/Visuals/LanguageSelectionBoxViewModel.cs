namespace ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;

    using TomsToolbox.Wpf;

    public class LanguageSelectionBoxViewModel : ObservableObject
    {
        public LanguageSelectionBoxViewModel(IEnumerable<CultureInfo> existingLanguages)
        {
            Languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();
        }

        [Required]
        public CultureInfo? SelectedLanguage { get; set; }

        public ICollection<CultureInfo>? Languages { get; }
    }
}
