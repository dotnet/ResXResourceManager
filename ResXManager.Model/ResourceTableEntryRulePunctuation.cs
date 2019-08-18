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
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public abstract string RuleId { get; }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract string Description { get; }

        public bool CheckRule(IEnumerable<string> values, out string message)
        {
            string reference = null;
            foreach (var value in values.Select(NormalizeUnicode).Select(GetCharIterator))
            {
                if (reference is null)
                {
                    StringBuilder sb = null;
                    foreach (var c in GetPunctuationSequence(value))
                        (sb ?? (sb = new StringBuilder())).Append(c);
                    reference = sb?.ToString() ?? string.Empty;
                }
                else if (!reference.SequenceEqual(GetPunctuationSequence(value)))
                {
                    message = GetErrorMessage(reference);
                    return false;
                }
            }

            message = null;
            return true;
        }

        [NotNull]
        protected abstract IEnumerable<char> GetCharIterator([NotNull] string value);

        [NotNull]
        protected abstract string GetErrorMessage([NotNull] string reference);

        [NotNull]
        private static string NormalizeUnicode([NotNull] string value) => value.Normalize();

        [NotNull]
        private static IEnumerable<char> GetPunctuationSequence([NotNull] IEnumerable<char> value)
        {
            return value.SkipWhile(char.IsWhiteSpace).
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
    }
}
