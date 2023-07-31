namespace ResXManager.View.Tools.Tests
{
    using Moq;
    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Tests;
    using ResXManager.View.Tools;
    using System.IO;
    using System.Threading.Tasks;
    using VerifyXunit;
    using Xunit;

    public class XlfSynchronizerTests
    {
        [UsesVerify]
        public class The_UpdateEntityFromXlf_Method
        {
            [Theory]
            [InlineData("fr")]
            [InlineData("nl")]
            public async Task Does_Not_Add_Empty_Lines(string languageToVerify)
            {
                var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ResXManager.Tests", "XlfSynchronizerTests", "Does_Not_Add_Empty_Lines");

                FileHelper.CopyDirectory(@".\Resources\Files\GH0504", directory);

                var configurationMock = new Mock<IConfiguration>();
                var tracerMock = new Mock<ITracer>();

                var projectFileName = Path.Combine(directory, "MyProject.resx");
                var projectFrFileName = Path.Combine(directory, "MyProject.fr.resx");
                var projectNlFileName = Path.Combine(directory, "MyProject.nl.resx");

                var fileNameToVerify = Path.Combine(directory, $"MyProject.{languageToVerify}.resx");
                var xlfDocumentPath = Path.Combine(directory, $"MyProject.{languageToVerify}.xlf");

                // Synchronize 3 times to see if generates the same file
                for (int i = 0; i < 3; i++)
                {
                    var resourceManager = new ResourceManager(configurationMock.Object, tracerMock.Object);

                    var projectFiles = new[]
                    {
                        new ProjectFile(projectFileName, directory, "MyProject", null),
                        new ProjectFile(projectFrFileName, directory, "MyProject", null),
                        new ProjectFile(projectNlFileName, directory, "MyProject", null)
                    };

                    var resourceEntity = new ResourceEntity(resourceManager, "MyProject", "MyProject", directory,
                        projectFiles, new System.Globalization.CultureInfo("en"), DuplicateKeyHandling.Fail);

                    resourceManager.ResourceEntities.Add(resourceEntity);

                    var xlfDocument = new XlfDocument(xlfDocumentPath);

                    using (var xlfSynchronizer = new XlfSynchronizer(resourceManager, tracerMock.Object, configurationMock.Object))
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
}
