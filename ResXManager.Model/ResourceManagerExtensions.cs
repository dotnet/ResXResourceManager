namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public static class ResourceManagerExtensions
    {
        /// <summary>
        /// Converts the culture name to the corresponding culture. An optional '.' prefix is removed before conversion.
        /// </summary>
        /// <param name="languageKey">Name of the culture, optionally preceded with a '.'.</param>
        /// <returns>The culture, or <c>null</c> if the conversion fails.</returns>
        public static CultureInfo ToCulture(this string languageKey)
        {
            try
            {
                return string.IsNullOrEmpty(languageKey) ? null : CultureInfo.GetCultureInfo(languageKey.TrimStart('.'));
            }
            catch (ArgumentException)
            {
            }

            throw new InvalidOperationException("Error parsing language: " + languageKey);
        }

        /// <summary>
        /// Converts the culture to the corresponding language key.
        /// </summary>
        /// <param name="culture">The culture.</param>
        /// <returns>The key.</returns>
        public static string ToLanguageKey(this CultureInfo culture)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return (culture == null) ? string.Empty : "." + culture.Name;
        }

        public static void Load(this ResourceManager resourceManager, string solutionFolder)
        {
            Contract.Requires(resourceManager != null);

            resourceManager.Load(GetAllSourceFiles(solutionFolder, file => false));
        }

        public static IList<ProjectFile> GetAllSourceFiles(string solutionFolder, Func<ProjectFile, bool> isSourceFileCallback)
        {
            Contract.Requires(isSourceFileCallback != null);

            if (string.IsNullOrEmpty(solutionFolder))
                solutionFolder = Directory.GetCurrentDirectory();

            var allProjectFiles = Directory.EnumerateFiles(solutionFolder, "*.*", SearchOption.AllDirectories)
                .Select(filePath => new ProjectFile(filePath, @"<unknown>", null))
                .Where(file => file.IsResourceFile() || isSourceFileCallback(file))
                .ToArray();

            var fileNamesByDirectory = allProjectFiles.GroupBy(file => file.GetBaseDirectory()).ToArray();

            foreach (var directoryFiles in fileNamesByDirectory)
            {
                if ((directoryFiles == null) || (directoryFiles.Key == null))
                    continue;

                var directory = new DirectoryInfo(directoryFiles.Key);
                var projectName = FindProjectName(directory);

                foreach (var file in directoryFiles.Where(file => file != null))
                {
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
