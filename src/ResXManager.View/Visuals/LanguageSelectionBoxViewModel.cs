namespace ResXManager.View.Visuals;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using PropertyChanged;

using TomsToolbox.Wpf;

public sealed class LanguageSelectionBoxViewModel : ObservableObject
{
    private readonly CultureInfo[] _availableCultures;

    public LanguageSelectionBoxViewModel(IEnumerable<CultureInfo> existingLanguages)
    {
        _availableCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
            .Except(existingLanguages)
            .OrderBy(culture => culture.DisplayName)
            .ToArray();

        Languages = new ListCollectionView(_availableCultures);
    }

    [OnChangedMethod(nameof(OnTextChanged))]
    public string? Text { get; set; }

    private void OnTextChanged(string? oldValue, string? newValue)
    {
        Languages.Filter = string.IsNullOrEmpty(newValue) ? null : (item) => FilterPredicate(item as CultureInfo, newValue);

        try
        {
            var culture = CultureInfo.GetCultureInfo(newValue);
            if (_availableCultures.Contains(culture))
            {
                SelectedLanguage = culture;
                return;
            }
        }
        catch
        {
            // text is not a valued culture id
        }

        SelectedLanguage = null;
    }

    [Required]
    public CultureInfo? SelectedLanguage { get; set; }

    public ICollectionView Languages { get; }

    private static bool FilterPredicate(CultureInfo? culture, string? selectedText) =>
        culture != null
        && (culture.Name.IndexOf(selectedText, StringComparison.OrdinalIgnoreCase) >= 0
            || culture.DisplayName.IndexOf(selectedText, StringComparison.OrdinalIgnoreCase) >= 0);
}