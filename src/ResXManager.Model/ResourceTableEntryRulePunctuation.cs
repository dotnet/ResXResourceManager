namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    using JetBrains.Annotations;

    using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

    public abstract class ResourceTableEntryRulePunctuation : IResourceTableEntryRule
    {
        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public abstract string RuleId { get; }

        public bool CompliesToRule(string? neutralValue, [NotNull, ItemCanBeNull] IEnumerable<string?> values, [NotNullWhen(false)] out string? message)
        {
            var reference = GetPunctuationSequence(neutralValue).ToArray();

            if (values.Select(GetPunctuationSequence).Any(value => !reference.SequenceEqual(value)))
            {
                message = GetErrorMessage(new string(reference));
                return false;
            }

            message = null;
            return true;
        }

        [JetBrains.Annotations.NotNull]
        protected abstract IEnumerable<char> GetCharIterator([JetBrains.Annotations.NotNull] string value);

        [JetBrains.Annotations.NotNull]
        protected abstract string GetErrorMessage([JetBrains.Annotations.NotNull] string reference);

        [JetBrains.Annotations.NotNull]
        private static string NormalizeUnicode(string? value) => value?.Normalize() ?? string.Empty;

        [JetBrains.Annotations.NotNull]
        private IEnumerable<char> GetPunctuationSequence(string? value)
        {
            return GetCharIterator(NormalizeUnicode(value))
                .SkipWhile(char.IsWhiteSpace).
                TakeWhile(IsPunctuation).
                Select(NormalizePunctuation);
        }

        private static char NormalizePunctuation(char value)
        {
            switch ((int)value)
            {
                case 0x055C: return '!'; // ARMENIAN EXCLAMATION MARK
                case 0x055D: return ','; // ARMENIAN COMMA
                case 0x055E: return '?'; // ARMENIAN QUESTION MARK
                case 0x0589: return '.'; // ARMENIAN FULL STOP
                case 0x07F8: return ','; // NKO COMMA
                case 0x07F9: return '!'; // NKO EXCLAMATION MARK
                case 0x1944: return '!'; // LIMBU EXCLAMATION MARK
                case 0x1945: return '?'; // LIMBU QUESTION MARK
                case 0x3001: return ','; // IDEOGRAPHIC COMMA
                case 0x3002: return '.'; // IDEOGRAPHIC FULL STOP
                case 0xFF01: return '!'; // FULLWIDTH EXCLAMATION MARK
                case 0xFF0C: return ','; // FULLWIDTH COMMA
                case 0xFF0E: return '.'; // FULLWIDTH FULL STOP
                case 0xFF1A: return ':'; // FULLWIDTH COLON
                case 0xFF1B: return ';'; // FULLWIDTH SEMICOLON
                case 0xFF1F: return '?'; // FULLWIDTH QUESTION MARK
                default: return value;
            }
        }

        /// <summary>
        /// This is used instead of <see cref="char.IsPunctuation(char)"/>, because the default
        /// method allows significantly more characters than required.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsPunctuation(char value)
        {
            // exclude quotes, special chars (\#), hot-key prefixes (&_), language specifics with no common equivalent (¡¿).
            const string excluded = "'\"\\#&_¡¿";

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (char.GetUnicodeCategory(value))
            {
                case UnicodeCategory.OtherPunctuation:
                    return !excluded.Contains(value);
                case UnicodeCategory.DashPunctuation:
                    return true;
                default:
                    return false;
            }
        }

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
