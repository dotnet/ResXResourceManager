namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;

    using tomenglertde.ResXManager.Model.Properties;
    
    [LocalizedDisplayName(StringResourceKey.ResourceTableEntryRulePunctuationLead_Name)]
    [LocalizedDescription(StringResourceKey.ResourceTableEntryRulePunctuationLead_Description)]
    internal sealed class ResourceTableEntryRulePunctuationLead : ResourceTableEntryRulePunctuation
    {
        internal const string PunctuationLead = "punctuationLead";

        public override string RuleId => PunctuationLead;

        protected override IEnumerable<char> GetCharIterator(string value) => value;

        protected override string GetErrorMessage(string reference)
        {
            var intro = Resources.ResourceTableEntryRulePunctuationLead_Error_Intro;
            if (string.IsNullOrEmpty(reference))
                return intro + " " + Resources.ResourceTableEntryRulePunctuationLead_Error_NoPunctuationExpected;

            return intro + " " + string.Format(Resources.Culture,
                Resources.ResourceTableEntryRulePunctuationLead_Error_PunctuationSeqExpected,
                reference);
        }
    }
}