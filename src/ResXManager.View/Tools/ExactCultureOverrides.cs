namespace ResXManager.View.Tools
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using TomsToolbox.Essentials;

    internal class ExactCultureOverrides
    {
        private const string DefaultOverrides = "fy-NL,";

        private readonly HashSet<CultureInfo> _overrides = ReadDefault().ToHashSet(_comparer);
        private static readonly IEqualityComparer<CultureInfo> _comparer = new DelegateEqualityComparer<CultureInfo>();
        public static readonly ExactCultureOverrides Exact = new();

        private ExactCultureOverrides()
        {
        }

        /// <summary>
        /// Checks if there should be a custom flag for the specific culture. Doesn't check actual file existence
        /// </summary>
        public bool HasCustomFlag(CultureInfo culture)
        {
            return _overrides.Contains(culture);
        }

        private static IEnumerable<CultureInfo> ReadDefault()
        {
            var exactCultureOverrides = DefaultOverrides.Split(',').Where(item => !string.IsNullOrEmpty(item));

            foreach (var item in exactCultureOverrides)
            {
                yield return CultureInfo.GetCultureInfo(item);
            }
        }
    }
}
