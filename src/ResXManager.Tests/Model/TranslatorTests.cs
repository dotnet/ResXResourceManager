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
    public void GoogleLiteParsesBatchedResponseCorrectly()
    {
        const string separator = "<resxmanager-batch-8d5f5a34-1/>";
        const string input = """
        [[["hallo\n","hello\n",null,null,3],["\u003cresxmanager-batch-8d5f5a34-1/\u003e\n","\u003cresxmanager-batch-8d5f5a34-1/\u003e\n",null,null,3],["Welt","world",null,null,3]],null,"en",null,null,null,null,[]]
        """;

        var result = Translators.GoogleTranslatorLite.ParseBatchResponse(input, [separator]);

        Assert.Equal(["hallo", "Welt"], result);
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
