namespace ResXManager.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;

    using Xunit;

    public class UnitTest1
    {
        [Fact]
        public void MefMigrationHelper()
        {
            string sourceFolder = Path.GetFullPath(@"..\..\..\..");

            var assemblyFileNames = Directory
                .EnumerateFiles(sourceFolder, "ResXManager*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains("resources"))
                .Where(path => !path.Contains("obj"))
                .Where(path => !path.Contains("Release"))
                .Distinct(new DelegateEqualityComparer<string>(Path.GetFileName));

            var targetFolder = Path.Combine(sourceFolder, @".migration\after");

            foreach (var assemblyFileName in assemblyFileNames)
            {
                var assembly = Assembly.LoadFrom(assemblyFileName);

                var result = MetadataReader.Read(assembly);

                var data = Serialize(result);

                var targetFile = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(assemblyFileName) + ".mef.json");

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.WriteAllText(targetFile, data);
            }
        }

        private static string Serialize(IList<ExportInfo> result)
        {
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}
