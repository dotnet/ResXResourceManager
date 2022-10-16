namespace ResXManager.Infrastructure
{
#if NETFRAMEWORK || NETSTANDARD
    using System;
    using System.Linq;

    public static class StringExtensions
    {

        public static string Replace(this string target, string oldValue, string? newValue, StringComparison _)
        {
            return target.Replace(oldValue, newValue);
        }

        public static bool Contains(this string target, char value, StringComparison _)
        {
            return target.Contains(value);
        }

    }
#endif
}
