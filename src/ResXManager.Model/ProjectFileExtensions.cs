namespace ResXManager.Model;

using System;
using System.Globalization;
using System.IO;
using System.Linq;

using ResXManager.Infrastructure;
using TomsToolbox.Essentials;

public static class ProjectFileExtensions
{
    private const string Resx = ".resx";
    private const string Resw = ".resw";
    private static readonly string[] _supportedFileExtensions = { Resx, Resw };

    public static string GetBaseDirectory(this ProjectFile projectFile)
    {
        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        var directoryName = Path.GetDirectoryName(Resw.Equals(extension, StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(filePath) : filePath);

        return directoryName ?? throw new InvalidOperationException();
    }

    public static bool IsSupportedFileExtension(string extension)
    {
        return _supportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsResourceFile(string filePath, string? extension = null)
    {
        extension ??= Path.GetExtension(filePath);

        if (!IsSupportedFileExtension(extension))
            return false;

        if (!Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
            return true;

        var languageName = Path.GetFileName(Path.GetDirectoryName(filePath));

        return CultureHelper.IsValidCultureName(languageName);
    }

    public static bool IsResourceFile(this ProjectFile projectFile)
    {
        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        return IsResourceFile(filePath, extension);
    }

    public static CultureKey GetCultureKey(this ProjectFile projectFile, CultureInfo neutralResourcesLanguage)
    {
        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var cultureName = Path.GetExtension(fileNameWithoutExtension).TrimStart('.');

            if (cultureName.IsNullOrEmpty())
                return CultureKey.Neutral;

            if (!CultureHelper.IsValidCultureName(cultureName))
                return CultureKey.Neutral;

            return new CultureKey(cultureName);
        }

        if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            var cultureName = Path.GetFileName(Path.GetDirectoryName(filePath));

            if (!CultureHelper.IsValidCultureName(cultureName))
                throw new ArgumentException(@"Invalid file. File name does not conform to the pattern '.\<cultureName>\<basename>.resw'");

            var culture = cultureName.ToCulture();

            return Equals(neutralResourcesLanguage, culture) ? CultureKey.Neutral : new CultureKey(culture);
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

        return CultureHelper.IsValidCultureName(languageName) ? Path.GetFileNameWithoutExtension(name) : name;
    }

    public static string GetLanguageFileName(this ProjectFile projectFile, CultureInfo culture)
    {
        return GetLanguageFileName(projectFile, null, culture);
    }

    public static string GetLanguageFileName(this ProjectFile projectFile, CultureKey? projectFileCultureKey, CultureInfo culture)
    {
        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            if (projectFileCultureKey != null && projectFileCultureKey.UseLCID)
            {
                // LCID based resource might not have a resource without any file extension. Therefore we need to remove two file extensions.
                // Example project file resource file: `Strings.1033.resx`.
                var fileNameWithoutResxExtension = Path.GetFileNameWithoutExtension(filePath);
                // Only remove second extension when it is a number (LCID).
                if (Path.HasExtension(fileNameWithoutResxExtension)
                    && int.TryParse(Path.GetExtension(fileNameWithoutResxExtension).TrimStart('.'), out var _))
                {
                    // Remove both extensions before adding the LCID extension.
                    return Path.ChangeExtension(Path.ChangeExtension(filePath, null), culture.LCID.ToString("D", CultureInfo.InvariantCulture)) + @".resx";
                }
                else
                {
                    return Path.ChangeExtension(filePath, culture.LCID.ToString("D", CultureInfo.InvariantCulture)) + @".resx";
                }
            }
            else
            {
                return Path.ChangeExtension(filePath, culture.ToString()) + @".resx";
            }
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
