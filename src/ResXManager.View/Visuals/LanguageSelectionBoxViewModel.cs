namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;

    public partial class LanguageSelectionBoxViewModel : INotifyPropertyChanged
    {
        private readonly IEnumerable<CultureInfo> _allLanguages;

        public LanguageSelectionBoxViewModel(IEnumerable<CultureInfo> existingLanguages)
        {
            _allLanguages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();
        }

        public string? LanguageFilter { get; set; }

        [Required]
        public CultureInfo? SelectedLanguage { get; set; }

        public IEnumerable<CultureInfo> Languages
        {
            get
            {
                return string.IsNullOrWhiteSpace(LanguageFilter)
                    ? _allLanguages
                    : _allLanguages.Where(r => r.Name.IndexOf(LanguageFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               r.DisplayName.IndexOf(LanguageFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
