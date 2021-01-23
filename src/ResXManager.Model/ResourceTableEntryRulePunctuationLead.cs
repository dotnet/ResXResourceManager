namespace ResXManager.Model
{
    using System.Collections.Generic;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;
    using TomsToolbox.Essentials;

    [LocalizedDisplayName(StringResourceKey.ResourceTableEntryRulePunctuationLead_Name)]
    [LocalizedDescription(StringResourceKey.ResourceTableEntryRulePunctuationLead_Description)]
    public sealed class ResourceTableEntryRulePunctuationLead : ResourceTableEntryRulePunctuation
    {
        public const string Id = "PunctuationLead";

        public override string RuleId => Id;

        protected override IEnumerable<char> GetCharIterator(string value) => value;

        protected override string GetErrorMessage(string reference)
        {
            var intro = Resources.ResourceTableEntryRulePunctuationLead_Error_Intro;
            if (reference.IsNullOrEmpty())
                return intro + " " + Resources.ResourceTableEntryRulePunctuationLead_Error_NoPunctuationExpected;

            return intro + " " + string.Format(Resources.Culture,
                Resources.ResourceTableEntryRulePunctuationLead_Error_PunctuationSeqExpected,
                reference);
        }
    }
}