namespace ResXManager.Tests.View.Tools;

using NSubstitute;
using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.View.Tools;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using ResXManager.Tests.Helpers;

using VerifyXunit;
using Xunit;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

public static class XlfSynchronizerTests
{
    public class The_UpdateEntityFromXlf_Method
    {
        [Theory]
        [InlineData("zh-Hans")]
        [InlineData("de")]
        public async Task Does_Not_Add_Empty_Lines(string languageToVerify)
        {
            var directory = Path.Combine(Path.GetTempPath(), "ResXManager.Tests", "XlfSynchronizerTests", "Does_Not_Add_Empty_Lines");

            FileHelper.CopyDirectory(@".\Resources\Files\GH0504", directory);

            var configurationMock = Substitute.For<IConfiguration>();
            var tracerMock = Substitute.For<ITracer>();

            var projectFileName = Path.Combine(directory, "MyProject.resx");
            var projectDeFileName = Path.Combine(directory, "MyProject.de.resx");
            var projectChnFileName = Path.Combine(directory, "MyProject.zh-Hans.resx");

            var fileNameToVerify = Path.Combine(directory, $"MyProject.{languageToVerify}.resx");
            var xlfDocumentPath = Path.Combine(directory, $"MyProject.{languageToVerify}.xlf");

            // Synchronize several times to see if generates the same file
            for (var i = 0; i < 6; i++)
            {
                var resourceManager = new ResourceManager(configurationMock, tracerMock);

                var projectFiles = new[]
                {
                    new ProjectFile(projectFileName, directory, "MyProject", null),
                    new ProjectFile(projectDeFileName, directory, "MyProject", null),
                    new ProjectFile(projectChnFileName, directory, "MyProject", null)
                };

                var resourceEntity = new ResourceEntity(resourceManager, "MyProject", "MyProject", directory,
                    projectFiles, new System.Globalization.CultureInfo("en"), DuplicateKeyHandling.Fail);

                resourceManager.ResourceEntities.Add(resourceEntity);

                var xlfDocument = new XlfDocument(xlfDocumentPath);

                using (var xlfSynchronizer = new XlfSynchronizer(resourceManager, tracerMock, configurationMock))
                {
                    xlfSynchronizer.UpdateEntityFromXlf(resourceEntity, xlfDocument.Files);
                }

                resourceManager.Save();
            }

            await Verifier.VerifyFile(fileNameToVerify)
                .UseParameters(languageToVerify);
        }

        [Fact]
        public async Task Throws_Exception_When_Duplicate_Resources_Are_Detected()
        {
            // Note: this is to reproduce a "bug" where xlf files can have a
            // target language that is the same as another file. In such case, 
            // it's very confusing what is happening. Therefore we purposely
            // created a duplicate file (fr and nl) that both have `fr` as 
            // target language.

            var directory = Path.Combine(Path.GetTempPath(), "ResXManager.Tests", "XlfSynchronizerTests", "Throws_Exception_When_Duplicate_Resources_Are_Detected");

            FileHelper.CopyDirectory(@".\Resources\Files\GH0666", directory);

            var configurationMock = Substitute.For<IConfiguration>();
            configurationMock.EnableXlifSync.Returns(true);

            var receivedTraceCalls = new List<string>();

            var tracerMock = Substitute.For<ITracer>();
            tracerMock.When(x => x.TraceError(Arg.Any<string>()))
                .Do(x => receivedTraceCalls.Add(x.ArgAt<string>(0)));

            var projectFileName = Path.Combine(directory, "MyProject.resx");
            var projectDeFileName = Path.Combine(directory, "MyProject.de.resx");
            var projectFrFileName = Path.Combine(directory, "MyProject.fr.resx");
            var projectNlFileName = Path.Combine(directory, "MyProject.nl.resx");
            var projectChnFileName = Path.Combine(directory, "MyProject.zh-Hans.resx");

            // Synchronize several times to see if generates the same file
            for (var i = 0; i < 6; i++)
            {
                var resourceManager = new ResourceManager(configurationMock, tracerMock);

                var projectFiles = new[]
                {
                    new ProjectFile(projectFileName, directory, "MyProject", null),
                    new ProjectFile(projectDeFileName, directory, "MyProject", null),
                    new ProjectFile(projectFrFileName, directory, "MyProject", null),
                    new ProjectFile(projectNlFileName, directory, "MyProject", null),
                    new ProjectFile(projectChnFileName, directory, "MyProject", null)
                };

                await resourceManager.ReloadAsync(directory, projectFiles, null);

                using (var xlfSynchronizer = new XlfSynchronizer(resourceManager, tracerMock, configurationMock))
                {
                    await xlfSynchronizer.UpdateFromXlfAsync();
                }

                resourceManager.Save();
            }

            await Verifier.Verify(receivedTraceCalls);
        }
    }
}
