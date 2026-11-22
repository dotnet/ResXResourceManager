namespace ResXManager.Tests.Model;

using System;
using System.Globalization;
using System.IO;
using System.Linq;

using NSubstitute;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.Tests.Helpers;

using Xunit;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

public class TranslationStateTests
{
    [Fact]
    public void Manual_Edit_Clears_Review_Marker_But_Untouched_Entry_Keeps_It()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration.MarkMachineTranslationsAsNeedsReview.Returns(true);

        var entity = CreateEntity(configuration);

        var entry1 = entity.Entries.Single(item => item.Key == "FirstResource");
        var entry2 = entity.Entries.Single(item => item.Key == "SecondResource");

        entry1.SetValueAndTranslationState(CultureKey.Parse("de"), "Hallo", TranslationState.NeedsReview);
        entry2.SetValueAndTranslationState(CultureKey.Parse("de"), "Welt", TranslationState.NeedsReview);

        Assert.Equal(TranslationState.NeedsReview, entry1.TranslationState.GetValue("de"));
        Assert.Equal(TranslationState.NeedsReview, entry2.TranslationState.GetValue("de"));

        entry1.Values.SetValue("de", "Hallo, Freund");

        Assert.Equal(TranslationState.Approved, entry1.TranslationState.GetValue("de"));
        Assert.Equal(TranslationState.NeedsReview, entry2.TranslationState.GetValue("de"));
    }

    [Fact]
    public void Manual_Edit_Does_Not_Touch_State_When_Setting_Is_Off()
    {
        var configuration = Substitute.For<IConfiguration>();

        var entity = CreateEntity(configuration);

        var entry = entity.Entries.Single(item => item.Key == "FirstResource");

        entry.Values.SetValue("de", "Hallo");
        entry.Values.SetValue("de", "Hallo, Freund");

        Assert.Equal(TranslationState.Approved, entry.TranslationState.GetValue("de"));
        Assert.DoesNotContain("@State", entry.Comments.GetValue("de") ?? string.Empty);
    }

    private static ResourceEntity CreateEntity(IConfiguration configuration)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ResXManager.Tests", "TranslationStateTests", Guid.NewGuid().ToString("N"));

        FileHelper.CopyDirectory(@".\Resources\Files\GH0762", directory);

        var tracer = Substitute.For<ITracer>();

        var resourceManager = new ResourceManager(configuration, tracer);

        return new ResourceEntity(resourceManager, "MyProject", "MyProject", directory,
            new[]
            {
                new ProjectFile(Path.Combine(directory, "MyProject.resx"), directory, "MyProject", null),
                new ProjectFile(Path.Combine(directory, "MyProject.de.resx"), directory, "MyProject", null)
            },
            new CultureInfo("en"), DuplicateKeyHandling.Fail);
    }
}
