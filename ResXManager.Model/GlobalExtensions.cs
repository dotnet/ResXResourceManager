namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using TomsToolbox.Core;

    /// <summary>
    /// Various extension methods to help generating better code.
    /// </summary>
    public static class GlobalExtensions
    {
        public static string ReplaceInvalidFileNameChars(this string value, char replacement)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            Path.GetInvalidFileNameChars().ForEach(c => value = value.Replace(c, replacement));

            return value;
        }
    }
}
