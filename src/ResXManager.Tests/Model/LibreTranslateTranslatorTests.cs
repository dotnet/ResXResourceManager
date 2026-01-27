namespace ResXManager.Tests.Model;

using System;
using System.Globalization;
using System.Linq;
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

        var result = Translators.LibreTranslateTranslator.ParseResponse(input).First();

        Assert.Equal("¡Hola!", result);
    }

    [Fact]
    public void LibreTranslateParsesResponseWithDetectedLanguageCorrectly()
    {
        const string input = "{\"detectedLanguage\":{\"confidence\":90.0,\"language\":\"fr\"},\"translatedText\":\"Hello!\"}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input).First();

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
    public void LibreTranslateParsesResponseWithAlternativesCorrectly()
    {
        const string input = "{\"translatedText\":\"Hi\",\"alternatives\":[\"Hello\",\"Hey\"]}";

        var result = Translators.LibreTranslateTranslator.ParseResponse(input);

        Assert.Equal(["Hi", "Hello", "Hey"], result);
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

    private const string LocalLibreUrl = "http://localhost:5000";

    private static async Task<bool> IsLibreTranslateAvailableAsync()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await httpClient.GetAsync($"{LocalLibreUrl}/languages");
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
            $"{LocalLibreUrl}/translate",
            apiKey: null,
            text: "Good morning",
            sourceLanguage: CultureInfo.GetCultureInfo("en"),
            targetLanguage: CultureInfo.GetCultureInfo("de"),
            alternatives: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEmpty(result[0]);

        // "Good morning" in German is typically "Guten Morgen"
        Assert.Contains("Guten Morgen", result[0], StringComparison.OrdinalIgnoreCase);
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
                LocalLibreUrl,
                apiKey: null,
                text: "Hello",
                sourceLanguage: CultureInfo.GetCultureInfo("en"),
                targetLanguage: CultureInfo.GetCultureInfo("es"),
                alternatives: null,
                cancellationToken: cts.Token));
    }

    #endregion
}
