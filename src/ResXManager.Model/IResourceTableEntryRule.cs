namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>A rule that is validated against a entry of the resource table.</summary>
    /// <remarks>
    /// This is used to implement the different rules that are used to check the proper translation.
    /// </remarks>
    public interface IResourceTableEntryRule : IResourceTableEntryRuleConfig
    {
        /// <summary>
        /// Check the rule by validating all the <paramref name="values" />.
        /// </summary>
        /// <param name="neutralValue">The neutral value.</param>
        /// <param name="values">The values to check</param>
        /// <param name="message">The human readable message that contains the reason for the failure.
        /// In case the rule check was successful this parameter will contain <see langword="null" />.</param>
        /// <returns>
        ///   <see langword="true" /> in case the values passed the check; otherwise <see langword="false" />
        /// </returns>
        bool CompliesToRule(string? neutralValue, IEnumerable<string?> values, [Localizable(true)][NotNullWhen(false)] out string? message);
    }

    public abstract class ResourceTableEntryRule : IResourceTableEntryRule
    {
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The identification of the rule. This value is used to disable the rule check.
        /// </summary>
        public abstract string RuleId { get; }

        public abstract bool CompliesToRule(string? neutralValue, IEnumerable<string?> values, [Localizable(true)][NotNullWhen(false)] out string? message);

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}