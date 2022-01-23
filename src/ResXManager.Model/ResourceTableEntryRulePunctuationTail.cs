﻿namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.Linq;

using ResXManager.Infrastructure;
using ResXManager.Model.Properties;
using TomsToolbox.Essentials;

[LocalizedDisplayName(StringResourceKey.ResourceTableEntryRulePunctuationTail_Name)]
[LocalizedDescription(StringResourceKey.ResourceTableEntryRulePunctuationTail_Description)]
public sealed class ResourceTableEntryRulePunctuationTail : ResourceTableEntryRulePunctuation
{
    public const string Id = "PunctuationTail";

    public override string RuleId => Id;

    protected override IEnumerable<char> GetCharIterator(string value) => value.Reverse();

    protected override string GetErrorMessage(string reference)
    {
        var intro = Resources.ResourceTableEntryRulePunctuationTail_Error_Intro;
        if (reference.IsNullOrEmpty())
            return intro + " " + Resources.ResourceTableEntryRulePunctuationTail_Error_NoPunctuationExpected;

        return intro + " " + string.Format(Resources.Culture,
                   Resources.ResourceTableEntryRulePunctuationTail_Error_PunctuationSeqExpected,
                   ReverseString(reference));
    }

    private static string ReverseString(string s)
    {
        var arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
