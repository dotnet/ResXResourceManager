namespace ResXManager.Model;

using System.ComponentModel;

/// <summary>A rule that is validated against a entry of the resource table.</summary>
/// <remarks>
/// This is used to implement the different rules that are used to check the proper translation.
/// </remarks>
public interface IResourceTableEntryRuleConfig : INotifyPropertyChanged
{
    /// <summary>
    /// This property is used to control if the rule is globally enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// The identification of the rule. This value is used to disable the rule check.
    /// </summary>
    string RuleId { get; }
}
