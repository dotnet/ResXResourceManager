namespace ResXManager.Model
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using ResXManager.Infrastructure;

    public static class ProjectFileExtensions
    {
        private const string Resx = ".resx";
        private const string Resw = ".resw";
        public static readonly string[] SupportedFileExtensions = { Resx, Resw };

        public static string GetBaseDirectory(this ProjectFile projectFile)
        {
            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            var directoryName = Path.GetDirectoryName(Resw.Equals(extension, StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(filePath) : filePath);

            return directoryName ?? throw new InvalidOperationException();
        }

        public static bool IsResourceFile(this ProjectFile projectFile)
        {
            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return false;

            if (!Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
                return true;

            var languageName = Path.GetFileName(Path.GetDirectoryName(filePath));

            return ResourceManager.IsValidLanguageName(languageName);
        }

        public static CultureKey GetCultureKey(this ProjectFile projectFile, IConfiguration configuration)
        {
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

                var culture = cultureName.ToCulture();

                return Equals(configuration.NeutralResourcesLanguage, culture) ? CultureKey.Neutral : new CultureKey(culture);
            }

            throw new InvalidOperationException("Unsupported file format: " + extension);
        }

        public static string GetBaseName(this ProjectFile projectFile)
        {
            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (!Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
                return Path.GetFileNameWithoutExtension(filePath);

            var name = Path.GetFileNameWithoutExtension(filePath);
            var innerExtension = Path.GetExtension(name);
            var languageName = innerExtension.TrimStart('.');

            return ResourceManager.IsValidLanguageName(languageName) ? Path.GetFileNameWithoutExtension(name) : name;
        }

        public static string GetLanguageFileName(this ProjectFile projectFile, CultureInfo culture)
        {
            var extension = projectFile.Extension;
            var filePath = projectFile.FilePath;

            if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return Path.ChangeExtension(filePath, culture.ToString()) + @".resx";
            }

            if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var languageFileName = Path.Combine(projectFile.GetBaseDirectory(), culture.ToString(), Path.GetFileName(filePath));
                return languageFileName;
            }

            throw new InvalidOperationException("Extension not supported: " + extension);
        }

        public static bool IsDesignerFile(this ProjectFile projectFile)
        {
            return projectFile.GetBaseName().EndsWith(".Designer", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVisualBasicFile(this ProjectFile projectFile)
        {
            return Path.GetExtension(projectFile.FilePath).Equals(".vb", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCSharpFile(this ProjectFile projectFile)
        {
            return Path.GetExtension(projectFile.FilePath).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
