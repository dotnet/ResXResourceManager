namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using TomsToolbox.Essentials;

    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<ResourceTableEntryRules>))]
    public sealed class ResourceTableEntryRules : INotifyChanged
    {
        public const string Default = @"{""EnabledRules"": [
""" + ResourceTableEntryRulePunctuationLead.PunctuationLead + @""",
""" + ResourceTableEntryRulePunctuationTail.PunctuationTail + @""",
""" + ResourceTableEntryRuleStringFormat.StringFormat + @""",
""" + ResourceTableEntryRuleWhiteSpaceLead.WhiteSpaceLead + @""",
""" + ResourceTableEntryRuleWhiteSpaceTail.WhiteSpaceTail + @"""
]}";

        [CanBeNull]
        [ItemNotNull]
        private IReadOnlyCollection<IResourceTableEntryRule> _rules;

        [NotNull]
        [ItemNotNull]
        private IReadOnlyCollection<IResourceTableEntryRule> Rules => _rules ?? (_rules = BuildRuleCollection());

        [NotNull]
        [ItemNotNull]
        public IReadOnlyCollection<IResourceTableEntryRuleConfig> ConfigurableRules => Rules;

        [NotNull]
        [ItemNotNull]
        [DataMember(Name = "EnabledRules")]
        public IEnumerable<string> EnabledRuleIds
        {
            get => ConfigurableRules.Where(r => r.IsEnabled).Select(r => r.RuleId);
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

        internal bool CompliesToRules([NotNull] [ItemNotNull] ICollection<string> mutedRules, string reference, string value, out IList<string> messages)
        {
            return CompliesToRules(mutedRules, reference, new[] { value }, out messages);
        }

        internal bool CompliesToRules([NotNull][ItemNotNull] ICollection<string> mutedRules, [CanBeNull] string reference, [NotNull, ItemCanBeNull] ICollection<string> values, out IList<string> messages)
        {
            var result = new List<string>();

            foreach (var rule in Rules.Where(r => r.IsEnabled && !mutedRules.Contains(r.RuleId)))
            {
                if (rule.CompliesToRule(reference, values, out var message))
                    continue;

                result.Add(message);
            }

            messages = result;

            return result.Count == 0;
        }

        public event EventHandler Changed;

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
