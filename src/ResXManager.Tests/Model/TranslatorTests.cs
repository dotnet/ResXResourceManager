namespace ResXManager.Tests.Model;

using System;

using Xunit;

public class TranslatorTests
{
    [Fact]
    public void GoogleLiteParsesFragmentedResponseCorrectly()
    {
        const string input = """
                             [[["Ei! ","Hey there! ",null,null,10],["Como tá indo? ","How's it going? ",null,null,10],["Isso é ótimo! ","That's great! ",null,null,10],["K, tchau","K, bye",null,null,3,null,null,[[]],[[["66379e56ded86dd057796dbeaebad517","en_pt_2023q1.md"]]]]],null,"en",null,null,null,null,[]]
                             """;

        var result = Translators.GoogleTranslatorLite.ParseResponse(input);

        Assert.Equal("Ei! Como tá indo? Isso é ótimo! K, tchau", result);
    }

    [Fact]
    public void GoogleLiteParsesInvalidResponseCorrectly()
    {
        const string input = $$"""
                               {"x":[["Ei! "]]}
                               """;

        var result = Translators.GoogleTranslatorLite.ParseResponse(input);

        Assert.Equal("", result);
    }

    [Fact]
    public void GoogleLiteThrowsOnBadJson()
    {
        const string input = $$"""
                               {"x":Ei!"]]}
                               """;

        Assert.ThrowsAny<Exception>(() => Translators.GoogleTranslatorLite.ParseResponse(input));
    }
}
