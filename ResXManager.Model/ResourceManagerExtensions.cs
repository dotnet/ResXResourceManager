namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Resource manager specific extension methods.
    /// </summary>
    public static class ResourceManagerExtensions
    {
        /// <summary>
        /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
        /// </summary>
        /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
        /// <returns>
        /// The culture, or <c>null</c> if the key name is empty.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Error parsing language:  + cultureKeyName</exception>
        public static CultureInfo ToCulture(this string cultureKeyName)
        {
            try
            {
                cultureKeyName = cultureKeyName.Maybe().Return(c => c.TrimStart('.'));
                
                return string.IsNullOrEmpty(cultureKeyName) ? null : CultureInfo.GetCultureInfo(cultureKeyName);
            }
            catch (ArgumentException)
            {
            }

            throw new InvalidOperationException("Error parsing language: " + cultureKeyName);
        }

        public static IList<ProjectFile> GetAllSourceFiles(this DirectoryInfo solutionFolder, Func<ProjectFile, bool> isSourceFileCallback)
        {
            Contract.Requires(solutionFolder != null);
            Contract.Requires(isSourceFileCallback != null);
            Contract.Ensures(Contract.Result<IList<ProjectFile>>() != null);

            var fileInfos = solutionFolder.EnumerateFiles("*.*", SearchOption.AllDirectories);
            Contract.Assume(fileInfos != null);

            var allProjectFiles = fileInfos
                .Select(fileInfo => new ProjectFile(fileInfo.FullName, solutionFolder.FullName, @"<unknown>", null))
                .Where(file => file.IsResourceFile() || isSourceFileCallback(file))
                .ToArray();

            var fileNamesByDirectory = allProjectFiles.GroupBy(file => file.GetBaseDirectory()).ToArray();

            foreach (var directoryFiles in fileNamesByDirectory)
            {
                if ((directoryFiles == null) || string.IsNullOrEmpty(directoryFiles.Key))
                    continue;

                var directory = new DirectoryInfo(directoryFiles.Key);
                var projectName = FindProjectName(directory);

                foreach (var file in directoryFiles)
                {
                    Contract.Assume(file != null);
                    file.ProjectName = projectName;
                }
            }

            return allProjectFiles;
        }

        private static string FindProjectName(DirectoryInfo directory)
        {
            Contract.Requires(directory != null);

            while (directory != null)
            {
                var projectFiles = directory.EnumerateFiles(@"*.*proj", SearchOption.TopDirectoryOnly);
                Contract.Assume(projectFiles != null);

                var project = projectFiles.FirstOrDefault();
                if (project != null)
                {
                    return Path.ChangeExtension(project.Name, null);
                }

                directory = directory.Parent;
            }

            return @"<no project>";
        }
    }
}
