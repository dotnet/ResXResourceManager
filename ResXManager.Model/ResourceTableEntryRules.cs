namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
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
        private ReadOnlyCollection<IResourceTableEntryRule> _rules;

        [NotNull]
        [ItemNotNull]
        private ReadOnlyCollection<IResourceTableEntryRule> Rules
        {
            get
            {
                var result = _rules;
                if (result != null) return result;

                result = BuildRuleCollection();
                foreach (var rule in result)
                    rule.PropertyChanged += (sender, args) => OnChanged();

                return (_rules = result);
            }
        }

        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<IResourceTableEntryRuleConfig> ConfigurableRules => Rules;

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
                    rule.IsEnabled = valueSet.Contains(rule.RuleId);
            }
        }

        private static ReadOnlyCollection<IResourceTableEntryRule> BuildRuleCollection()
        {
            var builder = new ReadOnlyCollectionBuilder<IResourceTableEntryRule>
            {
                new ResourceTableEntryRulePunctuationLead(),
                new ResourceTableEntryRulePunctuationTail(),
                new ResourceTableEntryRuleStringFormat(),
                new ResourceTableEntryRuleWhiteSpaceLead(),
                new ResourceTableEntryRuleWhiteSpaceTail()
            };

            // Init default values
            foreach (var rule in builder)
                rule.IsEnabled = true;

            return builder.ToReadOnlyCollection();
        }

        internal bool CheckRules([NotNull][ItemNotNull] ICollection<string> mutedRules, out IList<string> messages, [NotNull][ItemNotNull] params string[] values) =>
            CheckRules(mutedRules, values, out messages);

        internal bool CheckRules([NotNull][ItemNotNull] ICollection<string> mutedRules, [NotNull][ItemNotNull] IEnumerable<string> values, out IList<string> messages)
        {
            switch (values)
            {
                case ICollection<string> _:
                    return CheckRulesInternal(mutedRules, values, out messages);
                case IReadOnlyCollection<string> _:
                    return CheckRulesInternal(mutedRules, values, out messages);
                default:
                    return CheckRulesInternal(mutedRules, values.ToArray(), out messages);
            }
        }

        private bool CheckRulesInternal([NotNull][ItemNotNull] ICollection<string> mutedRules, [NotNull][ItemNotNull] IEnumerable<string> values, out IList<string> messages)
        {
            Debug.Assert(values is ICollection<string> || values is IReadOnlyCollection<string>);

            var result = new List<string>();
            foreach (var rule in Rules.Where(r => r.IsEnabled && !mutedRules.Contains(r.RuleId)))
            {
                // values is a buffered list, despite being stored in a IEnumerable
                // ReSharper disable once PossibleMultipleEnumeration
                if (rule.CheckRule(values, out var message)) continue;
                Debug.Assert(!string.IsNullOrEmpty(message));
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
