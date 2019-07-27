namespace tomenglertde.ResXManager.Model
{
    using System.IO;

    using JetBrains.Annotations;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Various extension methods to help generating better code.
    /// </summary>
    public static class GlobalExtensions
    {
        [NotNull]
        public static string ReplaceInvalidFileNameChars([NotNull] this string value, char replacement)
        {
            Path.GetInvalidFileNameChars().ForEach(c => value = value.Replace(c, replacement));

            return value;
        }
    }
}
