namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using JetBrains.Annotations;

    /// <summary>A rule that is validated against a entry of the resource table.</summary>
    /// <remarks>
    /// This is used to implement the different rules that are used to check the proper translation.
    /// </remarks>
    internal interface IResourceTableEntryRule : IResourceTableEntryRuleConfig
    {
        /// <summary>
        /// Check the rule by validating all the <paramref name="values"/>.
        /// </summary>
        /// <param name="values">The values to check</param>
        /// <param name="message">
        /// The human readable message that contains the reason for the failure.
        /// In case the rule check was successful this parameter will contain <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true"/> in case the values passed the check; otherwise <see langword="false"/>
        /// </returns>
        bool CompliesToRule([NotNull][ItemNotNull] IEnumerable<string> values,
            [CanBeNull][Localizable(true)] out string message);
    }
}