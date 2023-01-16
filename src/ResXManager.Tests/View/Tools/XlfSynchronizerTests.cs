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
            [Fact]
            public async Task Does_Not_Add_Empty_Lines()
            {
                var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ResXManager.Tests", "XlfSynchronizerTests", "Does_Not_Add_Empty_Lines");

                FileHelper.CopyDirectory(@".\Resources\Files\GH0504", directory);

                var configurationMock = new Mock<IConfiguration>();
                var tracerMock = new Mock<ITracer>();

                var resourceManager = new ResourceManager(configurationMock.Object, tracerMock.Object);

                var projectFileName = Path.Combine(directory, "MyProject.resx");
                var projectNlFileName = Path.Combine(directory, "MyProject.nl.resx");

                var projectFiles = new[]
                {
                    new ProjectFile(projectFileName, directory, "MyProject", null),
                    new ProjectFile(projectNlFileName, directory, "MyProject", null)
                };

                var resourceEntity = new ResourceEntity(resourceManager, "MyProject", "MyProject", directory,
                    projectFiles, new System.Globalization.CultureInfo("nl"), DuplicateKeyHandling.Fail);

                resourceManager.ResourceEntities.Add(resourceEntity);

                var xlfDocument = new XlfDocument(Path.Combine(directory, "MyProject.xlf"));

                // Synchronize 3 times to see if generates the same file
                for (int i = 0; i < 3; i++)
                {
                    using (var xlfSynchronizer = new XlfSynchronizer(resourceManager, tracerMock.Object, configurationMock.Object))
                    {
                        xlfSynchronizer.UpdateEntityFromXlf(resourceEntity, xlfDocument.Files);
                    }

                    resourceManager.Save();
                }

                await Verifier.VerifyFile(projectNlFileName);
            }
        }
    }
}
