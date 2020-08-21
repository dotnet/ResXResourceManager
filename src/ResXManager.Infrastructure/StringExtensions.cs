namespace ResXManager.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        #if NETFRAMEWORK

        public static string Replace(this string target, string oldValue, string? newValue, StringComparison _)
        {
            return target.Replace(oldValue, newValue);
        }

        public static bool Contains(this string target, char value, StringComparison _)
        {
            return target.Contains(value);
        }

        #endif
    }
}
