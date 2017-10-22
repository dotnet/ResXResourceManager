namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop;

    public class LanguageSelectionBoxViewModel : ObservableObject
    {
        public LanguageSelectionBoxViewModel([NotNull][ItemNotNull] IEnumerable<CultureInfo> existingLanguages)
        {
            Contract.Requires(existingLanguages != null);

            Languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();
        }

        [Required]
        public CultureInfo SelectedLanguage { get; set; }

        [ItemNotNull]
        public ICollection<CultureInfo> Languages { get; }
    }
}
