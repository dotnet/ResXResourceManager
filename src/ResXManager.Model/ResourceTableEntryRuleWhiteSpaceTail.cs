namespace ResXManager.Model;

using System.Collections.Generic;
using System.Linq;

using ResXManager.Infrastructure;
using ResXManager.Model.Properties;
using TomsToolbox.Essentials;

[LocalizedDisplayName(StringResourceKey.ResourceTableEntryRuleWhiteSpaceTail_Name)]
[LocalizedDescription(StringResourceKey.ResourceTableEntryRuleWhiteSpaceTail_Description)]
public sealed class ResourceTableEntryRuleWhiteSpaceTail : ResourceTableEntryRuleWhiteSpace
{
    public const string Id = "WhiteSpaceTail";

    public override string RuleId => Id;

    protected override IEnumerable<char> GetCharIterator(string? value) => value?.Reverse() ?? Enumerable.Empty<char>();

    protected override string GetErrorMessage(IEnumerable<string> reference)
    {
        var whiteSpaceSeq = string.Join("][", reference.Reverse());

        var intro = Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_Intro;
        if (whiteSpaceSeq.IsNullOrEmpty())
            return intro + " " + Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_NoWhiteSpaceExpected;

        whiteSpaceSeq = "[" + whiteSpaceSeq + "]";
        return intro + " " + string.Format(Resources.Culture,
            Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_WhiteSpaceSeqExpected,
            whiteSpaceSeq);
    }
}
