﻿namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

using TomsToolbox.Essentials;

[DataContract]
[TypeConverter(typeof(JsonSerializerTypeConverter<ResourceTableEntryRules>))]
public sealed class ResourceTableEntryRules : INotifyChanged
{
    private const string MutedRulePattern = @"@MutedRule\(([a-zA-Z]+)\)";
    private const string MutedRuleFormat = @"@MutedRule({0})";

    private static readonly Regex _mutedRuleExpression = new(MutedRulePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const string Default = @"{""EnabledRules"": [
""" + ResourceTableEntryRulePunctuationLead.Id + @""",
""" + ResourceTableEntryRulePunctuationTail.Id + @""",
""" + ResourceTableEntryRuleStringFormat.Id + @""",
""" + ResourceTableEntryRuleWhiteSpaceLead.Id + @""",
""" + ResourceTableEntryRuleWhiteSpaceTail.Id + @"""
]}";

    private IReadOnlyCollection<IResourceTableEntryRule>? _rules;

    private IDictionary<string, IResourceTableEntryRuleConfig>? _configurableRules;

    private IReadOnlyCollection<IResourceTableEntryRule> Rules => _rules ??= BuildRuleCollection();

    public IDictionary<string, IResourceTableEntryRuleConfig> ConfigurableRules => _configurableRules ??= new ReadOnlyDictionary<string, IResourceTableEntryRuleConfig>(Rules.Cast<IResourceTableEntryRuleConfig>().ToDictionary(rule => rule.RuleId));

    public bool IsEnabled(string? ruleId)
    {
        if (ruleId.IsNullOrEmpty())
            return false;

        return ConfigurableRules.GetValueOrDefault(ruleId)?.IsEnabled ?? false;
    }

    [DataMember(Name = "EnabledRules")]
    public IEnumerable<string> EnabledRuleIds
    {
        get => Rules.Where(r => r.IsEnabled).Select(r => r.RuleId);
        set
        {
            var valueSet = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
            foreach (var rule in Rules)
            {
                rule.IsEnabled = valueSet.Contains(rule.RuleId);
            }
        }
    }

    private IReadOnlyCollection<IResourceTableEntryRule> BuildRuleCollection()
    {
        var rules = new List<IResourceTableEntryRule>
            {
                new ResourceTableEntryRuleStringFormat(),
                new ResourceTableEntryRuleWhiteSpaceLead(),
                new ResourceTableEntryRuleWhiteSpaceTail(),
                new ResourceTableEntryRulePunctuationLead(),
                new ResourceTableEntryRulePunctuationTail(),
            };

        // Init default values
        foreach (var rule in rules)
        {
            rule.IsEnabled = true;
            rule.PropertyChanged += (sender, args) => OnChanged();
        }

        return rules.AsReadOnly();
    }

    internal bool CompliesToRules(ICollection<string> mutedRuleIds, string? reference, string? value, out IList<string> messages)
    {
        return CompliesToRules(mutedRuleIds, reference, new[] { value }, out messages);
    }

    internal bool CompliesToRules(ICollection<string> mutedRuleIds, string? reference, ICollection<string?> values, out IList<string> messages)
    {
        var result = new List<string>();

        foreach (var rule in Rules.Where(r => r.IsEnabled && !mutedRuleIds.Contains(r.RuleId)))
        {
            if (rule.CompliesToRule(reference, values, out var message))
                continue;

            result.Add(message);
        }

        messages = result;

        return result.Count == 0;
    }

    public event EventHandler? Changed;

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal static IEnumerable<string> GetMutedRuleIds(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return Enumerable.Empty<string>();

        var rules = _mutedRuleExpression
            .Matches(comment)
            .Cast<Match>()
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value);

        return rules;
    }

    internal static string SetMutedRuleIds(string? comment, ISet<string> mutedRuleIds)
    {
        var commentBuilder = new StringBuilder(comment ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(comment))
        {
            // Existing comment. We may need to strip old muting values.
            var matches = _mutedRuleExpression.Matches(comment)
                .Cast<Match>()
                .Where(match => match.Success)
                .Reverse();

            foreach (var match in matches)
            {
                if (!mutedRuleIds.Remove(match.Groups[1].Value))
                {
                    commentBuilder.Remove(match.Index, match.Length);
                }
            }
        }

        foreach (var rule in mutedRuleIds)
        {
            commentBuilder.AppendFormat(CultureInfo.InvariantCulture, MutedRuleFormat, rule);
        }

        return commentBuilder.ToString();
    }
}
