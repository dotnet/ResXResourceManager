namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;

    using tomenglertde.ResXManager.Model.Properties;

    internal sealed class ResourceTableEntryRuleWhiteSpaceLead : ResourceTableEntryRuleWhiteSpace
    {
        internal const string WhiteSpaceLead = "whiteSpaceLead";

        public override string RuleId => WhiteSpaceLead;
        public override string Name => Resources.ResourceTableEntryRuleWhiteSpaceLead_Name;
        public override string Description => Resources.ResourceTableEntryRuleWhiteSpaceLead_Description;

        protected override IEnumerable<char> GetCharIterator(string value) => value;

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
