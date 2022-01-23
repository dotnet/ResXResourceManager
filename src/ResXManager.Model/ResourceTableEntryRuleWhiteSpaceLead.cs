namespace ResXManager.Model;

using System.Collections.Generic;
using System.Linq;

using ResXManager.Infrastructure;
using ResXManager.Model.Properties;
using TomsToolbox.Essentials;

[LocalizedDisplayName(StringResourceKey.ResourceTableEntryRuleWhiteSpaceLead_Name)]
[LocalizedDescription(StringResourceKey.ResourceTableEntryRuleWhiteSpaceLead_Description)]
public sealed class ResourceTableEntryRuleWhiteSpaceLead : ResourceTableEntryRuleWhiteSpace
{
    public const string Id = "WhiteSpaceLead";

    public override string RuleId => Id;

    protected override IEnumerable<char> GetCharIterator(string? value) => value ?? Enumerable.Empty<char>();

    protected override string GetErrorMessage(IEnumerable<string> reference)
    {
        var whiteSpaceSeq = string.Join("][", reference);

        var intro = Resources.ResourceTableEntryRuleWhiteSpaceLead_Error_Intro;
        if (whiteSpaceSeq.IsNullOrEmpty())
            return intro + " " + Resources.ResourceTableEntryRuleWhiteSpaceLead_Error_NoWhiteSpaceExpected;

        whiteSpaceSeq = "[" + whiteSpaceSeq + "]";
        return intro + " " + string.Format(Resources.Culture,
            Resources.ResourceTableEntryRuleWhiteSpaceLead_Error_WhiteSpaceSeqExpected,
            whiteSpaceSeq);
    }
}
