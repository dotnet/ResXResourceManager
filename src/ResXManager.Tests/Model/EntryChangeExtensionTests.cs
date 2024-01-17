namespace ResXManager.Tests.Model;

using ResXManager.Model;

using Xunit;

public class EntryChangeExtensionTests
{
    [Theory]
    [InlineData("a", "b", true)]
    [InlineData("abc", "abc", false)]
    [InlineData(null, "b", true)]
    [InlineData(null, "", false)]
    [InlineData("", "", false)]
    [InlineData("", null, false)]
    public void IsModifiedWorks(string? left, string? right, bool expected)
    {
        var result = EntryChangeExtensions.IsModified(left, right);

        Assert.Equal(expected, result);
    }
}