namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    public static class ProjectFileExtensions
    {
        private const string Resx = ".resx";
        private const string Resw = ".resw";
        [NotNull]
        [ItemNotNull]
        public static readonly string[] SupportedFileExtensions = { Resx, Resw };

        [NotNull]
        public static string GetBaseDirectory([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            var directoryName = Path.GetDirectoryName(Resw.Equals(extension, StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(filePath) : filePath);
            Contract.Assume(!string.IsNullOrEmpty(directoryName));

            return directoryName;
        }

        public static bool IsResourceFile([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return false;

            if (!Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
                return true;

            var languageName = Path.GetFileName(Path.GetDirectoryName(filePath));

            return ResourceManager.IsValidLanguageName(languageName);
        }

        [NotNull]
        public static CultureKey GetCultureKey([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<CultureKey>() != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                var cultureName = Path.GetExtension(fileNameWithoutExtension).TrimStart('.');

                if (string.IsNullOrEmpty(cultureName))
                    return CultureKey.Neutral;

                if (!ResourceManager.IsValidLanguageName(cultureName))
                    return CultureKey.Neutral;

                return new CultureKey(cultureName);
            }

            if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var cultureName = Path.GetFileName(Path.GetDirectoryName(filePath));

                if (!ResourceManager.IsValidLanguageName(cultureName))
                    throw new ArgumentException(@"Invalid file. File name does not conform to the pattern '.\<cultureName>\<basename>.resw'");

                return new CultureKey(cultureName);
            }

            throw new InvalidOperationException("Unsupported file format: " + extension);
        }

        [NotNull]
        public static string GetBaseName([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
                return Path.GetFileNameWithoutExtension(filePath);

            var name = Path.GetFileNameWithoutExtension(filePath);
            var innerExtension = Path.GetExtension(name);
            var languageName = innerExtension.TrimStart('.');

            return ResourceManager.IsValidLanguageName(languageName) ? Path.GetFileNameWithoutExtension(name) : name;
        }

        [NotNull]
        public static string GetLanguageFileName([NotNull] this ProjectFile projectFile, [NotNull] CultureInfo culture)
        {
            Contract.Requires(projectFile != null);
            Contract.Requires(culture != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return Path.ChangeExtension(filePath, culture.ToString()) + @".resx";
            }

            if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var languageFileName = Path.Combine(projectFile.GetBaseDirectory(), culture.ToString(), Path.GetFileName(filePath));
                Contract.Assume(!string.IsNullOrEmpty(languageFileName));
                return languageFileName;
            }

            throw new InvalidOperationException("Extension not supported: " + extension);
        }

        public static bool IsDesignerFile([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            return projectFile.GetBaseName().EndsWith(".Designer", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVisualBasicFile([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            return Path.GetExtension(projectFile.FilePath).Equals(".vb", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCSharpFile([NotNull] this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            return Path.GetExtension(projectFile.FilePath).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
