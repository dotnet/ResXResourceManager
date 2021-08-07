namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using ResXManager.Model.Properties;
    using TomsToolbox.Essentials;

    [LocalizedDisplayName(StringResourceKey.ResourceTableEntryRuleStringFormat_Name)]
    [LocalizedDescription(StringResourceKey.ResourceTableEntryRuleStringFormat_Description)]
    public sealed class ResourceTableEntryRuleStringFormat : ResourceTableEntryRule
    {
        public const string Id = "StringFormat";

        public override string RuleId => Id;

        public override bool CompliesToRule(string? neutralValue, IEnumerable<string?> values, [NotNullWhen(false)] out string? message)
        {
            if (CompliesToRule(neutralValue, values))
            {
                message = null;
                return true;
            }

            message = Resources.ResourceTableEntry_Error_StringFormatParameterMismatch;
            return false;
        }

        private static bool CompliesToRule(string? neutralValue, IEnumerable<string?> values)
        {
            var allValues = new[] { neutralValue }.Concat(values.Where(value => !value.IsNullOrEmpty())).ToList();

            var indexedComply = allValues
                       .Select(GetStringFormatByIndexFlags)
                       .Distinct()
                       .Count() <= 1;

            var namedComply = allValues
                       .Select(GetStringFormatByPlaceholdersFingerprint)
                       .Distinct()
                       .Count() <= 1;

            return indexedComply && namedComply;
        }

        private static readonly Regex _getStringFormatByIndexExpression = new(@"\{([0-9]+)(?:,-?[0-9]+)?(?::[^\}]+)?\}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static long GetStringFormatByIndexFlags(string? value)
        {
            if (value.IsNullOrEmpty())
                return 0;

            return _getStringFormatByIndexExpression.Matches(value)
                .Cast<Match>()
                .Where(m => m.Success)
                .Aggregate(0L, (a, match) => a | ParseMatch(match));
        }

        private static string GetStringFormatByPlaceholdersFingerprint(string? value)
        {
            if (value.IsNullOrEmpty())
                return string.Empty;

            return string.Join("|", WebFilesExporter.ExtractPlaceholders(value).OrderBy(item => item));
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
