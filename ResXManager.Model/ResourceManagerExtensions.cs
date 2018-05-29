namespace tomenglertde.ResXManager.Model
{
    using JetBrains.Annotations;

    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Resource manager specific extension methods.
    /// </summary>
    public static class ResourceManagerExtensions
    {
        [NotNull]
        [ItemNotNull]
        public static IList<ProjectFile> GetAllSourceFiles([NotNull] this DirectoryInfo solutionFolder, [NotNull] IFileFilter fileFilter)
        {
            Contract.Requires(solutionFolder != null);
            Contract.Requires(fileFilter != null);
            Contract.Ensures(Contract.Result<IList<ProjectFile>>() != null);

            var solutionFolderLength = solutionFolder.FullName.Length + 1;

            var fileInfos = solutionFolder.EnumerateFiles("*.*", SearchOption.AllDirectories);
            Contract.Assume(fileInfos != null);

            var allProjectFiles = fileInfos
                .Where(fileFilter.IncludeFile)
                .Select(fileInfo => new ProjectFile(fileInfo.FullName, solutionFolder.FullName, @"<unknown>", null))
                .Where(file => file.IsResourceFile() || fileFilter.IsSourceFile(file))
                .ToArray();

            var fileNamesByDirectory = allProjectFiles.GroupBy(file => file.GetBaseDirectory()).ToArray();

            foreach (var directoryFiles in fileNamesByDirectory)
            {
                var directoryPath = directoryFiles?.Key;

                if (string.IsNullOrEmpty(directoryPath))
                    continue;

                var directory = new DirectoryInfo(directoryPath);
                var project = FindProject(directory, solutionFolder.FullName);

                var uniqueProjectName = (string)null;

                if (project != null)
                {
                    var projectName = Path.ChangeExtension(project.Name, null);
                    
                    var fullProjectName = project.FullName;
                    if (fullProjectName.Length >= solutionFolderLength) // project found is in solution tree
                    {
                        uniqueProjectName = fullProjectName.Substring(solutionFolderLength);
                    }

                    foreach (var file in directoryFiles)
                    {
                        Contract.Assume(file != null);
                        file.ProjectName = projectName;
                        file.AssemblyName = GetValueOrDefaultFromCsProj(project, "AssemblyName", projectName);
                        file.RootNamespace = GetValueOrDefaultFromCsProj(project, "RootNamespace", projectName);
                        file.UniqueProjectName = uniqueProjectName;
                    }
                }
            }

            return allProjectFiles;
        }

        [CanBeNull]
        private static FileInfo FindProject([NotNull] DirectoryInfo directory, [NotNull] string solutionFolder)
        {
            Contract.Requires(directory != null);
            Contract.Requires(solutionFolder != null);

            while ((directory != null) && (directory.FullName.Length >= solutionFolder.Length))
            {
                var projectFiles = directory.EnumerateFiles(@"*.*proj", SearchOption.TopDirectoryOnly);
                Contract.Assume(projectFiles != null);

                var project = projectFiles.FirstOrDefault();
                if (project != null)
                {
                    return project;
                }

                directory = directory.Parent;
            }

            return null;
        }

        /// <summary>
        ///     Loads an element's content from the csproj file. If element is not present, the <see cref="defaultValue"/> is taken.
        /// </summary>
        public static string GetValueOrDefaultFromCsProj(FileInfo csProjectFileInfo, string elementName, string defaultValue)
        {
            Contract.Requires(csProjectFileInfo != null);
            Contract.Requires(!string.IsNullOrEmpty(elementName));
            
            //Try to read value of element from the .csproj-File
            var content = File.ReadAllText(csProjectFileInfo.FullName);
            Regex r = new Regex($"(<{elementName}>)(.*)(</{elementName}>)");
            Match match = r.Match(content);
            if (!match.Success || match.Groups.Count <= 3)
            {
                return defaultValue;
            }
            return match.Groups[2].Value;
        }
    }
}
