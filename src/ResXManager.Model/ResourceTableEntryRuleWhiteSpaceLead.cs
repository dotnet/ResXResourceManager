namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using ResXManager.Model.Properties;
    
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
            if (string.IsNullOrEmpty(whiteSpaceSeq))
                return intro + " " + Resources.ResourceTableEntryRuleWhiteSpaceLead_Error_NoWhiteSpaceExpected;

            whiteSpaceSeq = "[" + whiteSpaceSeq + "]";
            return intro + " " + string.Format(Resources.Culture,
                Resources.ResourceTableEntryRuleWhiteSpaceLead_Error_WhiteSpaceSeqExpected,
                whiteSpaceSeq);
        }
    }
}
