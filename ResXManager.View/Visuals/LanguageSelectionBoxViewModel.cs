namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;

    using TomsToolbox.Desktop;

    public class LanguageSelectionBoxViewModel : ObservableObject
    {
        private CultureInfo _selectedLanguage;

        public LanguageSelectionBoxViewModel(IEnumerable<CultureInfo> existingLanguages)
        {
            Languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();


        }

        [Required]
        public CultureInfo SelectedLanguage
        {
            get
            {
                return _selectedLanguage;
            }
            set
            {
                SetProperty(ref _selectedLanguage, value, nameof(SelectedLanguage));
            }
        }

        public ICollection<CultureInfo> Languages { get; }

    }
}
