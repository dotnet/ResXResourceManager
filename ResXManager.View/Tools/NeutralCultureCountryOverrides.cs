namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class NeutralCultureCountryOverrides
    {
        private const string DefaultOverrides = "en=en-US,zh=zh-CN,zh-CHT=zh-CN,zh-HANT=zh-CN,";
        private static readonly IEqualityComparer<KeyValuePair<CultureInfo, CultureInfo>> _comparer = new DelegateEqualityComparer<KeyValuePair<CultureInfo, CultureInfo>>(item => item.Key);

        [NotNull]
        private readonly Dictionary<CultureInfo, CultureInfo> _overrides = new Dictionary<CultureInfo, CultureInfo>(ReadSettings().Distinct(_comparer).ToDictionary(item => item.Key, item => item.Value));

        [NotNull]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly NeutralCultureCountryOverrides Default = new NeutralCultureCountryOverrides();

        private NeutralCultureCountryOverrides()
        {
        }

        public event EventHandler<CultureOverrideEventArgs> OverrideChanged;

        public CultureInfo this[[NotNull] CultureInfo neutralCulture]
        {
            get
            {
                Contract.Requires(neutralCulture != null);

                if (!_overrides.TryGetValue(neutralCulture, out CultureInfo specificCulture))
                {
                    specificCulture = GetDefaultSpecificCulture(neutralCulture);
                }

                return specificCulture;
            }
            set
            {
                Contract.Requires(neutralCulture != null);
                Contract.Requires(value != null);

                if (value.Equals(GetDefaultSpecificCulture(neutralCulture)))
                {
                    _overrides.Remove(neutralCulture);
                }
                else
                {
                    _overrides[neutralCulture] = value;
                }

                OnOverrideChanged(new CultureOverrideEventArgs(neutralCulture, value));
                WriteSettings();
            }
        }

        private void OnOverrideChanged(CultureOverrideEventArgs e)
        {
            OverrideChanged?.Invoke(this, e);
        }

        private static CultureInfo GetDefaultSpecificCulture([NotNull] CultureInfo neutralCulture)
        {
            Contract.Requires(neutralCulture != null);

            var cultureName = neutralCulture.Name;
            var specificCultures = neutralCulture.GetDescendants().ToArray();

            var preferredSpecificCultureName = cultureName + @"-" + cultureName.ToUpperInvariant();

            var specificCulture =
                // If a specific culture exists with "subtag == primary tag" (e.g. de-DE), use this
                specificCultures.FirstOrDefault(c => c.Name.Equals(preferredSpecificCultureName, StringComparison.OrdinalIgnoreCase))
                // else it's more likely that the default one starts with the same letter as the neutral culture name (sv-SE, not sv-FI)
                ?? specificCultures.FirstOrDefault(c => c.Name.Split('-').Last().StartsWith(cultureName.Substring(0, 1), StringComparison.OrdinalIgnoreCase))
                // If nothing else matches, use the first.
                ?? specificCultures.FirstOrDefault();

            return specificCulture;
        }

        [NotNull]
        private static IEnumerable<KeyValuePair<CultureInfo, CultureInfo>> ReadSettings()
        {
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<CultureInfo, CultureInfo>>>() != null);

            var neutralCultureCountryOverrides = (DefaultOverrides + Settings.Default.NeutralCultureCountyOverrides).Split(',');

            foreach (var item in neutralCultureCountryOverrides)
            {
                CultureInfo neutralCulture;
                CultureInfo specificCulture;

                try
                {
                    var parts = item.Split('=').Select(i => i.Trim()).ToArray();
                    if (parts.Length != 2)
                        continue;

                    neutralCulture = CultureInfo.GetCultureInfo(parts[0]);
                    specificCulture = CultureInfo.GetCultureInfo(parts[1]);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                yield return new KeyValuePair<CultureInfo, CultureInfo>(neutralCulture, specificCulture);
            }
        }

        private void WriteSettings()
        {
            var items = _overrides.Select(item => string.Join("=", item.Key, item.Value));
            var settings = string.Join(",", items);

            Settings.Default.NeutralCultureCountyOverrides = settings;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_overrides != null);
        }
    }
}
