namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
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
            Contract.Requires(!String.IsNullOrEmpty(groupName));
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
            Contract.Requires(!String.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<string>() != null);

            return element.Attribute(name).Maybe().Return(x => x.Value, string.Empty);
        }

        public static DependencyPropertyEventWrapper<T> Track<T>(this T dependencyObject, DependencyProperty property)
            where T : DependencyObject
        {
            Contract.Requires(dependencyObject != null);
            Contract.Requires(property != null);
            Contract.Ensures(Contract.Result<DependencyPropertyEventWrapper<T>>() != null);

            return new DependencyPropertyEventWrapper<T>(dependencyObject, property);
        }

        public static IEnumerable<CultureInfo> GetAncestors(this CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);
            Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);

            var item = cultureInfo.Parent;

            while (!string.IsNullOrEmpty(item.Name))
            {
                yield return item;
                item = item.Parent;
            }
        }
        public static IEnumerable<CultureInfo> GetAncestorsOfSelf(this CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);
            Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);

            var item = cultureInfo;

            while (!string.IsNullOrEmpty(item.Name))
            {
                yield return item;
                item = item.Parent;
            }
        }

        private static readonly Dictionary<CultureInfo, CultureInfo[]> _childCache = new Dictionary<CultureInfo, CultureInfo[]>();

        public static ICollection<CultureInfo> GetChildren(this CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);
            Contract.Ensures(Contract.Result<ICollection<CultureInfo>>() != null);

            var children = _childCache.ForceValue(cultureInfo, CreateChildList);
            Contract.Assume(children != null); // because CreateChildList always returns != null
            return children;
        }

        private static CultureInfo[] CreateChildList(CultureInfo parent)
        {
            Contract.Ensures(Contract.Result<CultureInfo[]>() != null);

            return CultureInfo.GetCultures(CultureTypes.AllCultures).Where(child => child.Parent.Equals(parent)).ToArray();
        }

        public static IEnumerable<CultureInfo> GetDescendents(this CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);
            Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);

            foreach (var child in cultureInfo.GetChildren())
            {
                yield return child;

                foreach (var d in child.GetDescendents())
                {
                    yield return d;
                }
            }
        }
    }

    public class DependencyPropertyEventWrapper<T>
    {
        private readonly T _dependencyObject;
        private readonly DependencyPropertyDescriptor _dependencyPropertyDescriptor;

        public DependencyPropertyEventWrapper(T dependencyObject, DependencyProperty property)
        {
            Contract.Requires(dependencyObject != null);
            Contract.Requires(property != null);

            _dependencyObject = dependencyObject;
            _dependencyPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(T));
            Contract.Assume(_dependencyPropertyDescriptor != null);
        }

        public event EventHandler Changed
        {
            add
            {
                Contract.Requires(value != null);
                _dependencyPropertyDescriptor.AddValueChanged(_dependencyObject, value);
            }
            remove
            {
                Contract.Requires(value != null);
                _dependencyPropertyDescriptor.RemoveValueChanged(_dependencyObject, value);
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_dependencyObject != null);
            Contract.Invariant(_dependencyPropertyDescriptor != null);
        }
    }
}
