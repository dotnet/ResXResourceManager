namespace ResXManager.Model;

using System;
using System.Globalization;
using System.Text.RegularExpressions;

using ResXManager.Infrastructure;

public static class ResourceTableEntryExtensions
{
    private const string InvariantKey = "@Invariant";

    private static readonly Regex StateCommentRegex = new(@"@State\((\w+)\)");
    private const string StateCommentFormat = @"@State({0})";

    public static bool GetIsInvariant(this string? comment)
    {
        return comment?.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string? WithIsInvariant(this string? comment, bool value)
    {
        if (value)
        {
            if (!(comment?.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return comment + InvariantKey;
            }
        }
        else
        {
            return comment?.Replace(InvariantKey, string.Empty, StringComparison.Ordinal);
        }

        return comment;
    }

    public static TranslationState? GetTranslationState(this string? comment)
    {
        var match = StateCommentRegex.Match(comment ?? string.Empty);
        if (!match.Success)
            return null;

        if (!Enum.TryParse<TranslationState>(match.Groups[1].Value, out var value))
            return null;

        return value;
    }

    public static string? WithTranslationState(this string? originalComment, TranslationState? state)
    {
        var comment = originalComment == null ? null : StateCommentRegex.Replace(originalComment, string.Empty).Trim();

        if (state != null)
        {
            comment += " " + string.Format(CultureInfo.InvariantCulture, StateCommentFormat, state);
        }

        return comment;
    }

    public static void DecomposeCommentTokens(this string? comment, out string commentText, out TranslationState? translationState, out bool isInvariant)
    {
        isInvariant = false;
        translationState = default;
        commentText = comment ?? string.Empty;

        if (string.IsNullOrEmpty(commentText))
            return;

        var match = StateCommentRegex.Match(commentText);
        if (match.Success)
        {
            if (Enum.TryParse<TranslationState>(match.Groups[1].Value, out var value))
            {
                translationState = value;
            }

            commentText = commentText.Substring(0, match.Index) + commentText.Substring(match.Index + match.Length);
        }

#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf' => not available in NETFRAMEWORK
        if (commentText.IndexOf(InvariantKey, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            isInvariant = true;
            commentText = commentText.Replace(InvariantKey, string.Empty, StringComparison.Ordinal);
        }

        commentText = commentText.Trim();
    }
}