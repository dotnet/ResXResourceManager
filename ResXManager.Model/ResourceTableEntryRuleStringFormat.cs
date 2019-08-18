namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model.Properties;

    internal sealed class ResourceTableEntryRuleStringFormat : IResourceTableEntryRule
    {
        internal const string StringFormat = "stringFormat";

        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public string RuleId => StringFormat;

        /// <inheritdoc />
        public string Name => Resources.ResourceTableEntryRuleStringFormat_Name;

        /// <inheritdoc />
        public string Description => Resources.ResourceTableEntryRuleStringFormat_Description;

        public bool CheckRule(IEnumerable<string> values, out string message)
        {
            if (CheckRule(values))
            {
                message = null;
                return true;
            }

            message = Resources.ResourceTableEntry_Error_StringFormatParameterMismatch;
            return false;
        }

        private static bool CheckRule([NotNull][ItemNotNull] IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value))
                .Select(GetStringFormatFlags)
                .Distinct()
                .Count() <= 1;

        private static long GetStringFormatFlags([CanBeNull] string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            const string pattern = @"\{([0-9]+)(?:,-?[0-9]+)?(?::\S+)?\}";
            const RegexOptions options = RegexOptions.CultureInvariant;

            return Regex.Matches(value, pattern, options)
                .Cast<Match>()
                .Where(m => m.Success)
                .Aggregate(0L, (a, match) => a | ParseMatch(match));
        }

        private static long ParseMatch(Match match)
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return 1L << value;

            Debug.Fail("Unexpected parsing failure.", $"Regular expression matched {match.Groups[1].Value} as number, but parsing integer failed.");
            return 0;
        }
    }
}
