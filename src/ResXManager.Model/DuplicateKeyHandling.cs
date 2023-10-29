namespace ResXManager.Model;

using ResXManager.Model.Properties;

public enum DuplicateKeyHandling
{
    [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Rename)]
    Rename,
    [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Fail)]
    Fail
}