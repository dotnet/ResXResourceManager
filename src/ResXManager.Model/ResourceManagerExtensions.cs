﻿namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Resource manager specific extension methods.
    /// </summary>
    public static class ResourceManagerExtensions
    {
        public static IList<ProjectFile> GetAllSourceFiles(this DirectoryInfo solutionFolder, IFileFilter fileFilter, CancellationToken? cancellationToken)
        {
            void EnumerationShouldContinue()
            {
                cancellationToken?.ThrowIfCancellationRequested();
            }

            var solutionFolderLength = solutionFolder.FullName.Length + 1;

            var fileInfos = solutionFolder.EnumerateSourceFiles();

            var allProjectFiles = fileInfos
                .Select(item => item.Intercept(_ => EnumerationShouldContinue()))
                .Select(fileInfo => new ProjectFile(fileInfo.FullName, solutionFolder.FullName, @"<unknown>", null))
                .Where(fileFilter.Matches)
                .ToList();

            var fileNamesByDirectory = allProjectFiles.GroupBy(file => file.GetBaseDirectory()).ToList();

            foreach (var directoryFiles in fileNamesByDirectory)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                var directoryPath = directoryFiles?.Key;

                if (directoryPath.IsNullOrEmpty())
                    continue;

                var directory = new DirectoryInfo(directoryPath);
                var project = FindProject(directory, solutionFolder.FullName);

                var projectName = directory.Name;
                string? uniqueProjectName = null;

                if (project != null)
                {
                    projectName = Path.ChangeExtension(project.Name, null);

                    var fullProjectName = project.FullName;
                    if (fullProjectName.Length >= solutionFolderLength) // project found is in solution tree
                    {
                        uniqueProjectName = fullProjectName.Substring(solutionFolderLength);
                    }
                }

                // ! GroupBy always returns a non-empty collection
                foreach (var file in directoryFiles!)
                {
                    file.ProjectName = projectName;
                    file.UniqueProjectName = uniqueProjectName;
                }
            }

            return allProjectFiles;
        }

        private static FileInfo? FindProject(DirectoryInfo? directory, string solutionFolder)
        {
            while ((directory is not null) && (directory.FullName.Length >= solutionFolder.Length))
            {
                var projectFiles = directory.EnumerateFiles(@"*.*proj", SearchOption.TopDirectoryOnly);

                var project = projectFiles.FirstOrDefault();
                if (project is not null)
                {
                    return project;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
