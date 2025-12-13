namespace ResXManager.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using TomsToolbox.Essentials;

public static class ExtensionMethods
{
    public static string? NullIfEmpty(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return value;
    }

    /// <summary>
    /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
    /// </summary>
    /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
    /// <returns>
    /// The culture, or <c>null</c> if the key name is empty.
    /// </returns>
    /// <exception cref="InvalidOperationException">Error parsing language:  + cultureKeyName</exception>
    public static CultureInfo? ToCulture(this string? cultureKeyName)
    {
        try
        {
            return CultureHelper.CreateCultureInfo(cultureKeyName?.TrimStart('.').NullIfEmpty());
        }
        catch (ArgumentException)
        {
        }

        throw new InvalidOperationException("Error parsing language: " + cultureKeyName);
    }

    /// <summary>
    /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
    /// </summary>
    /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
    /// <returns>
    /// The cultureKey, or <c>null</c> if the culture is invalid.
    /// </returns>
    public static CultureKey? ToCultureKey(this string? cultureKeyName)
    {
        try
        {
            return ToCulture(cultureKeyName);
        }
        catch (ArgumentException)
        {
        }

        return null;
    }

    public static Regex? TryCreateRegex(this string? expression)
    {
        try
        {
            if (!expression.IsNullOrEmpty())
                return new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        }
        catch
        {
            // invalid expression, ignore...
        }

        return null;
    }

    /// <summary>
    /// Tests whether the string contains HTML
    /// </summary>
    /// <returns>
    /// True if the contains HTML; otherwise false
    /// </returns>
    public static bool ContainsHtml(this string text)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(text);
        return doc.DocumentNode.Descendants().Any(n => n.NodeType != HtmlNodeType.Text);
    }

    private static readonly HashSet<string> _excludedDirectories = new(new[] { "bin", "obj", "node_modules" }, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<FileInfo> EnumerateSourceFiles(this DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles())
        {
            yield return file;
        }

        foreach (var subDirectory in directory.EnumerateDirectories())
        {
            var name = subDirectory.Name;
            if (name.StartsWith(".", StringComparison.Ordinal) || _excludedDirectories.Contains(name))
                continue;

            foreach (var file in EnumerateSourceFiles(subDirectory))
            {
                yield return file;
            }
        }
    }
}
