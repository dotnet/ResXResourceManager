namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public static class ProjectFileExtensions
    {
        private const string Resx = ".resx";
        private const string Resw = ".resw";
        private static readonly string[] SupportedFileExtensions = { Resx, Resw };

        public static string GetBaseDirectory(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            return Path.GetDirectoryName(Resw.Equals(extension, StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(filePath) : filePath);
        }

        public static bool IsResourceFile(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return false;

            if (!Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
                return true;

            var languageName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;

            return ResourceManager.IsValidLanguageName(languageName);
        }

        public static string GetLanguageName(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                var languageName = Path.GetExtension(fileNameWithoutExtension) ?? string.Empty;

                if ((languageName.Length > 0) && !ResourceManager.IsValidLanguageName(languageName.TrimStart('.')))
                    return string.Empty;

                return languageName;
            }

            if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var languageName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;

                if (!ResourceManager.IsValidLanguageName(languageName))
                    throw new ArgumentException(
                        @"Invalid file. File name does not conform to the pattern '.\<languangeName>\<basename>.resx'");

                return "." + languageName;
            }

            throw new InvalidOperationException("Unsupported file format: " + extension);
        }

        public static string GetBaseName(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!Resx.Equals(extension, StringComparison.OrdinalIgnoreCase)) 
                return Path.GetFileNameWithoutExtension(filePath);

            var name = Path.GetFileNameWithoutExtension(filePath);
            var innerExtension = Path.GetExtension(name) ?? string.Empty;
            var languageName = innerExtension.TrimStart('.');

            return ResourceManager.IsValidLanguageName(languageName) ? Path.GetFileNameWithoutExtension(name) : name;
        }


        public static string GetLanguageFileName(this ProjectFile projectFile, CultureInfo language)
        {
            Contract.Requires(projectFile != null);
            Contract.Requires(language != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return Path.ChangeExtension(filePath, language.ToString()) + @".resx";
            }

            if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(projectFile.GetBaseDirectory(), language.ToString(), Path.GetFileName(filePath));
            }

            throw new InvalidOperationException("Extension not supported: " + extension);
        }

        public static bool IsDesignerFile(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            return projectFile.GetBaseName().EndsWith(".Designer", StringComparison.OrdinalIgnoreCase); 
        }
    }
}
