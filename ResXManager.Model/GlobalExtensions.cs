namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Interactivity;
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

        /// <summary>
        /// Replaces the invalid file name chars in the string with the replacement.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="replacement">The replacement of invalid file name chars. Can be null or empty to remove all invalid file name chars.</param>
        /// <returns>The value with all invalid file name chars replaced.</returns>
        public static string ReplaceInvalidFileNameChars(this string value, string replacement)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (value.Length <= 0)
                return value;

            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var index = 0;

            while ((index = value.IndexOfAny(invalidFileNameChars, index)) >= 0)
            {
                Contract.Assume(index + 1 < value.Length);
                value = value.Remove(index, 1);

                if (string.IsNullOrEmpty(replacement))
                    continue;

                value = value.Insert(index, replacement);
                index += replacement.Length;
            }

            return value;
        }

        public static T ForceBehavior<T>(this DependencyObject item)
            where T : Behavior, new()
        {
            Contract.Ensures(Contract.Result<T>() != null);

            var behaviors = Interaction.GetBehaviors(item);
            Contract.Assume(behaviors != null);

            var behavior = behaviors.OfType<T>().FirstOrDefault();
            if (behavior != null)
                return behavior;

            behavior = new T();


            behaviors.Add(behavior);

            return behavior;
        }

        [ContractVerification(false)]
        public static string TryGetAttribute(this XElement element, string name)
        {
            Contract.Requires(element != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<string>() != null);

            return element.Attribute(name).Maybe().Return(x => x.Value, string.Empty);
        }
    }
}
