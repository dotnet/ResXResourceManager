namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Linq;

    using tomenglertde.ResXManager.Model.Properties;

    internal sealed class ResourceTableEntryRuleWhiteSpaceTail : ResourceTableEntryRuleWhiteSpace
    {
        internal const string WhiteSpaceTail = "whiteSpaceTail";

        public override string RuleId => WhiteSpaceTail;
        public override string Name => Resources.ResourceTableEntryRuleWhiteSpaceTail_Name;
        public override string Description => Resources.ResourceTableEntryRuleWhiteSpaceTail_Description;

        protected override IEnumerable<char> GetCharIterator(string value)
        {
            for (var i = value.Length - 1; i >= 0; i--)
                yield return value[i];
        }

        protected override string GetErrorMessage(IEnumerable<string> reference)
        {
            var whiteSpaceSeq = string.Join("][", reference.Reverse());

            var intro = Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_Intro;
            if (string.IsNullOrEmpty(whiteSpaceSeq))
                return intro + " " + Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_NoWhiteSpaceExpected;

            whiteSpaceSeq = "[" + whiteSpaceSeq + "]";
            return intro + " " + string.Format(Resources.Culture,
                Resources.ResourceTableEntryRuleWhiteSpaceTail_Error_WhiteSpaceSeqExpected,
                whiteSpaceSeq);
        }
    }
}
