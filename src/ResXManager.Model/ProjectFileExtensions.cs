namespace ResXManager.Model;

using System;
using System.Globalization;
using System.IO;
using System.Linq;

using ResXManager.Infrastructure;

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

    public static CultureDefinition GetCultureDefinition(this ProjectFile projectFile, CultureInfo neutralResourcesLanguage)
    {
        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var cultureName = Path.GetExtension(fileNameWithoutExtension).TrimStart('.');

            if (!CultureHelper.IsValidCultureName(cultureName))
                return new(CultureKey.Neutral, CultureRepresentation.Name);

            var cultureKey = new CultureKey(cultureName);

            if (!int.TryParse(cultureName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lcid))
                return new(cultureKey, CultureRepresentation.Name);

            if (Equals(neutralResourcesLanguage, CultureInfo.GetCultureInfo(lcid)))
            {
                return new(CultureKey.Neutral, CultureRepresentation.Lcid);
            }

            return new(cultureKey, CultureRepresentation.Lcid);

        }

        if (Resw.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            var cultureName = Path.GetFileName(Path.GetDirectoryName(filePath));

            if (!CultureHelper.IsValidCultureName(cultureName))
                throw new ArgumentException(@"Invalid file. File name does not conform to the pattern '.\<cultureName>\<basename>.resw'");

            var culture = cultureName.ToCulture();

            var cultureKey = Equals(neutralResourcesLanguage, culture) ? CultureKey.Neutral : new CultureKey(culture);

            return new(cultureKey, CultureRepresentation.Name);
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

    public static string GetLanguageFileName(this ResourceLanguage neutralLanguage, CultureInfo culture)
    {
        var projectFile = neutralLanguage.ProjectFile;

        var extension = projectFile.Extension;
        var filePath = projectFile.FilePath;

        if (Resx.Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            var baseName = Path.ChangeExtension(filePath, null);
            string cultureTag;

            if (neutralLanguage.CultureRepresentation == CultureRepresentation.Lcid)
            {
                // Neutral LCID based resource also includes a language tag in the file, e.g. "Strings.1033.resx" - else we would not have classified it as LCID based.
                baseName = Path.ChangeExtension(baseName, null);
                cultureTag = culture.LCID.ToString("D", CultureInfo.InvariantCulture);
            }
            else
            {
                cultureTag = culture.Name;
            }

            return $"{baseName}.{cultureTag}.resx";
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
