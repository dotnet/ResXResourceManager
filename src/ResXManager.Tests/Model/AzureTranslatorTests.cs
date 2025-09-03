namespace ResXManager.Tests.Model;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ResXManager.Infrastructure;
using ResXManager.Translators;
using Xunit;

public class AzureTranslatorTests
{
    private static Uri InvokeCreateUriWithSettings(AzureTranslator sut, ITranslationSession session, Uri endpoint, CultureInfo targetLanguage, string textType)
    {
        var method = typeof(AzureTranslator).GetMethod("CreateUriWithSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object[] args = [session, endpoint, targetLanguage, textType];
        var result = method.Invoke(sut, args);
        var uri = Assert.IsType<Uri>(result);
        return uri;
    }

    private static ITranslationSession CreateSession(CultureInfo sourceCulture)
    {
        return new TranslationSessionStub(sourceCulture);
    }

    [Fact]
    public void WhenCustomCategoryIdIsSet_ShouldAddArgumentToRestRequest()
    {
        var sut = new AzureTranslator
        {
            CustomCategoryId = "my-custom-category"
        };

        var session = CreateSession(CultureInfo.GetCultureInfo("en"));
        var endpoint = new Uri("https://example.com/");
        var target = CultureInfo.GetCultureInfo("de-DE");

        var uri = InvokeCreateUriWithSettings(sut, session, endpoint, target, "plain");

        Assert.Contains("category=my-custom-category", uri.Query);
    }

    [Fact]
    public void WhenCustomCategoryIdIsNotSet_ShouldNotAddArgumentToRestRequest()
    {
        var sut = new AzureTranslator
        {
            CustomCategoryId = string.Empty
        };

        var session = CreateSession(CultureInfo.GetCultureInfo("en"));
        var endpoint = new Uri("https://example.com/");
        var target = CultureInfo.GetCultureInfo("fr-FR");

        var uri = InvokeCreateUriWithSettings(sut, session, endpoint, target, "plain");

        Assert.DoesNotContain("category=", uri.Query);
    }

    [Fact]
    public void WhenCustomCategoryIdIsWhitespace_ShouldNotAddArgumentToRestRequest()
    {
        var sut = new AzureTranslator
        {
            CustomCategoryId = "  \t  "
        };

        var session = CreateSession(CultureInfo.GetCultureInfo("en"));
        var endpoint = new Uri("https://example.com/");
        var target = CultureInfo.GetCultureInfo("it-IT");

        var uri = InvokeCreateUriWithSettings(sut, session, endpoint, target, "plain");

        Assert.DoesNotContain("category=", uri.Query);
    }

    [Fact]
    public void WhenTextTypeIsHtml_ShouldAddTextTypeHtml()
    {
        var sut = new AzureTranslator();
        var session = CreateSession(CultureInfo.GetCultureInfo("en"));
        var endpoint = new Uri("https://example.com/");
        var target = CultureInfo.GetCultureInfo("es-ES");

        var uri = InvokeCreateUriWithSettings(sut, session, endpoint, target, "html");

        Assert.Contains("textType=html", uri.Query);
    }

    [Fact]
    public void WhenLanguagesProvided_ShouldUseIetfLanguageTagsForFromAndTo()
    {
        var sut = new AzureTranslator();
        var session = CreateSession(CultureInfo.GetCultureInfo("en-US"));
        var endpoint = new Uri("https://example.com/");
        var target = CultureInfo.GetCultureInfo("de-DE");

        var uri = InvokeCreateUriWithSettings(sut, session, endpoint, target, "plain");

        Assert.Contains("from=en-US", uri.Query);
        Assert.Contains("to=de-DE", uri.Query);
    }

    [Fact]
    public void WhenSaveCredentialsToggles_SerializedAuthenticationKeyShouldRespectFlag()
    {
        var sut = new AzureTranslator
        {
            SaveCredentials = false,
            SerializedAuthenticationKey = "secret"
        };

        Assert.Null(sut.SerializedAuthenticationKey);

        sut.SaveCredentials = true;
        Assert.Equal("secret", sut.SerializedAuthenticationKey);
    }

    [Fact]
    public void WhenTranslatorIsCreated_DefaultEndpointShouldBeSet()
    {
        var sut = new AzureTranslator();
        Assert.Equal("https://api.cognitive.microsofttranslator.com", sut.Endpoint);
    }

    private sealed class TranslationSessionStub : ITranslationSession
    {
        public TranslationSessionStub(CultureInfo sourceLanguage)
        {
            SourceLanguage = sourceLanguage;
        }

        public bool IsActive => false;
        public bool IsCanceled => false;
        public bool IsComplete => false;
        public CancellationToken CancellationToken => CancellationToken.None;
        public void Cancel() { }
        public ICollection<ITranslationItem> Items => Array.Empty<ITranslationItem>();
        public IList<string> Messages => Array.Empty<string>();
        public CultureInfo NeutralResourcesLanguage => CultureInfo.InvariantCulture;
        public int Progress { get; set; }
        public CultureInfo SourceLanguage { get; }
        public TaskFactory MainThread => Task.Factory;
        public void AddMessage(string text) { }
        public void Dispose() { }
    }
}
