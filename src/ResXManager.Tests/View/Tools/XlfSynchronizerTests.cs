namespace ResXManager.View.Tools.Tests;

using NSubstitute;
using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.Tests;
using ResXManager.View.Tools;
using System.IO;
using System.Threading.Tasks;
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
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ResXManager.Tests", "XlfSynchronizerTests", "Does_Not_Add_Empty_Lines");

            FileHelper.CopyDirectory(@".\Resources\Files\GH0504", directory);

            var configurationMock = Substitute.For<IConfiguration>();
            var tracerMock = Substitute.For<ITracer>();

            var projectFileName = Path.Combine(directory, "MyProject.resx");
            var projectDeFileName = Path.Combine(directory, "MyProject.de.resx");
            var projectChnFileName = Path.Combine(directory, "MyProject.zh-Hans.resx");

            var fileNameToVerify = Path.Combine(directory, $"MyProject.{languageToVerify}.resx");
            var xlfDocumentPath = Path.Combine(directory, $"MyProject.{languageToVerify}.xlf");

            // Synchronize several times to see if generates the same file
            for (int i = 0; i < 6; i++)
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
    }
}