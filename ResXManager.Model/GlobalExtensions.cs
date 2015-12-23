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
        /// <summary>
        /// Retrieve the value from a match while checking contract rules.
        /// </summary>
        /// <param name="match">The <see cref="Match"/>.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns>The value of the match group.</returns>
        public static string GetGroupValue(this Match match, string groupName)
        {
            Contract.Requires(match != null);
            Contract.Requires(!string.IsNullOrEmpty(groupName));
            Contract.Ensures(Contract.Result<string>() != null);

            var group = match.Groups[groupName];
            Contract.Assume(group != null);

            var value = group.Value;
            Contract.Assert(value != null);

            return value;
        }

        [ContractVerification(false)]
        public static string TryGetAttribute(this XElement element, string name)
        {
            Contract.Requires(element != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<string>() != null);

            return element.Attribute(name).Maybe().Return(x => x.Value, string.Empty);
        }

        public static string ReplaceInvalidFileNameChars(this string value, char replacement)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            Path.GetInvalidFileNameChars().ForEach(c => value = value.Replace(c, replacement));

            return value;
        }
    }
}
