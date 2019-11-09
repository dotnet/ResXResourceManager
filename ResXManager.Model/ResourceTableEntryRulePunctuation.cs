namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    internal abstract class ResourceTableEntryRulePunctuation : IResourceTableEntryRule
    {
        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public abstract string RuleId { get; }

        public bool CompliesToRule([CanBeNull] string neutralValue, [NotNull, ItemCanBeNull] IEnumerable<string> values, [CanBeNull] out string message)
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

        [NotNull]
        protected abstract IEnumerable<char> GetCharIterator([NotNull] string value);

        [NotNull]
        protected abstract string GetErrorMessage([NotNull] string reference);

        [NotNull]
        private static string NormalizeUnicode([CanBeNull] string value) => value?.Normalize() ?? string.Empty;

        [NotNull]
        private IEnumerable<char> GetPunctuationSequence([CanBeNull] string value)
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
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (char.GetUnicodeCategory(value))
            {
                case UnicodeCategory.OtherPunctuation:
                    return value != '"' && value != '\'';
                case UnicodeCategory.DashPunctuation:
                    return true;
                default:
                    return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
