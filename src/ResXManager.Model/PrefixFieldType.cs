namespace ResXManager.Model;

using System;

using ResXManager.Model.Properties;

[Flags]
public enum PrefixFieldType
{
    /// <summary>
    /// Apply translation prefix to the value
    /// </summary>
    [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeValue)]
    Value = 1,
    /// <summary>
    /// Apply translation prefix to comment of neutral language
    /// </summary>
    [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeComment)]
    Comment = 2,
    /// <summary>
    /// Apply translation prefix to comment of target language
    /// </summary>
    [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeTargetComment)]
    TargetComment = 4,
}

public static class ExtensionMethods
{
    public static bool IsFlagSet(this PrefixFieldType target, PrefixFieldType flag)
    {
        return (target & flag) != 0;
    }

    public static PrefixFieldType WithFlag(this PrefixFieldType target, PrefixFieldType flag, bool value)
    {
        return value ? target | flag : target & ~flag;
    }
}