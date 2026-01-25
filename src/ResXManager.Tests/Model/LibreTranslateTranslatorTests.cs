namespace ResXManager.Tests.Model;

using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

public class LibreTranslateTranslatorTests
{
    [Fact]
    public void LibreTranslateParsesResponseCorrectly()
    {
        const string input = "{\"translatedText\":\"¡Hola!\"}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input);

        Assert.Equal("¡Hola!", result);
    }

    [Fact]
    public void LibreTranslateParsesResponseWithDetectedLanguageCorrectly()
    {
        const string input = "{\"detectedLanguage\":{\"confidence\":90.0,\"language\":\"fr\"},\"translatedText\":\"Hello!\"}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input);

        Assert.Equal("Hello!", result);
    }

    [Fact]
    public void LibreTranslateParsesEmptyResponseCorrectly()
    {
        const string input = "{}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input);

        Assert.Null(result);
    }

    [Fact]
    public void LibreTranslateParsesNullTranslatedTextCorrectly()
    {
        const string input = "{\"translatedText\":null}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input);

        Assert.Null(result);
    }

    [Fact]
    public void LibreTranslateThrowsOnBadJson()
    {
        const string input = """
                             {"translatedText":
                             """;

        Assert.ThrowsAny<Exception>(() => Translators.LibreTranslateTranslator.ParseResponse(input));
    }

    #region Integration Tests (require local LibreTranslate instance on localhost:5000)

    private const string LocalLibreTranslateUrl = "http://localhost:5000";

    private static async Task<bool> IsLibreTranslateAvailableAsync()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await httpClient.GetAsync($"{LocalLibreTranslateUrl}/languages");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task TranslateEnglishToGerman_ReturnsTranslatedText()
    {
        if (!await IsLibreTranslateAvailableAsync())
        {
            // Skip test if LibreTranslate is not running locally
            return;
        }

        var result = await Translators.LibreTranslateTranslator.TranslateAsync(
            LocalLibreTranslateUrl,
            apiKey: null,
            text: "Good morning",
            sourceLanguage: CultureInfo.GetCultureInfo("en"),
            targetLanguage: CultureInfo.GetCultureInfo("de"),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // "Good morning" in German is typically "Guten Morgen"
        Assert.Contains("Guten Morgen", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateWithCancellation_ThrowsOperationCanceledException()
    {
        if (!await IsLibreTranslateAvailableAsync())
        {
            // Skip test if LibreTranslate is not running locally
            return;
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Translators.LibreTranslateTranslator.TranslateAsync(
                LocalLibreTranslateUrl,
                apiKey: null,
                text: "Hello",
                sourceLanguage: CultureInfo.GetCultureInfo("en"),
                targetLanguage: CultureInfo.GetCultureInfo("es"),
                cancellationToken: cts.Token));
    }

    #endregion
}
