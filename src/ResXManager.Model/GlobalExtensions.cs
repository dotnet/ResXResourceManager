﻿namespace ResXManager.Model;

using System.IO;

using TomsToolbox.Essentials;

/// <summary>
/// Various extension methods to help generating better code.
/// </summary>
public static class GlobalExtensions
{
    public static string ReplaceInvalidFileNameChars(this string value, char replacement)
    {
        Path.GetInvalidFileNameChars().ForEach(c => value = value.Replace(c, replacement));

        return value;
    }

    public static bool Matches(this IFileFilter filter, ProjectFile file)
    {
        return filter.IncludeFile(file) && (file.IsResourceFile() || filter.IsSourceFile(file));
    }
}
