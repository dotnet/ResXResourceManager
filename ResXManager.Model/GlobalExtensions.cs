namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Threading;

    /// <summary>
    /// Various extension methods to help generating better code.
    /// </summary>
    public static class GlobalExtensions
    {
        /// <summary>
        /// Returns an enumeration of exceptions that contains this exception and all inner exceptions.
        /// </summary>
        /// <param name="ex">The exception to start with.</param>
        /// <returns>The exception and all inner exceptions.</returns>
        public static IEnumerable<Exception> ExceptionChain(this Exception ex)
        {
            while (ex != null)
            {
                yield return ex;
                ex = ex.InnerException;
            }
        }

        /// <summary>
        /// Gets the custom assemblies referenced by the assembly of the specified type.
        /// </summary>
        /// <param name="entryType">A type contained in the entry assembly.</param>
        /// <returns>The assembly that contains the entryType plus all custom assemblies that this assembly references.</returns>
        public static IEnumerable<Assembly> GetCustomAssemblies(this Type entryType)
        {
            Contract.Requires(entryType != null);
            Contract.Ensures(Contract.Result<IEnumerable<Assembly>>() != null);

            var entryAssembly = entryType.Assembly;

            var programFolder = Path.GetDirectoryName(new Uri(entryAssembly.CodeBase).LocalPath);
            Contract.Assert(programFolder != null);

            var referencedAssemblyNames = entryAssembly.GetReferencedAssemblies();

            var referencedAssemblies = referencedAssemblyNames
                .Select(Assembly.Load)
                .Where(assembly => assembly.GetName().IsAssemblyInSubfolderOf(programFolder));

            return new[] { entryAssembly }.Concat(referencedAssemblies);
        }

        /// <summary>
        /// Synchronizes the lists items with another lists items. The order of the items is ignored.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="target">The list to synchronize.</param>
        /// <param name="source">The items that should be in the target list.</param>
        public static void SynchronizeWith<T>(this ICollection<T> target, ICollection<T> source)
        {
            Contract.Requires(target != null);
            Contract.Requires(source != null);

            var removedItems = target.Except(source).ToArray();
            var addedItems = source.Except(target).ToArray();

            target.RemoveRange(removedItems);
            target.AddRange(addedItems);
        }

        /// <summary>
        /// Removes a range of elements from the List.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="items">The items to remove.</param>
        public static void RemoveRange(this IList target, IEnumerable items)
        {
            Contract.Requires(target != null);
            Contract.Requires(items != null);

            foreach (var i in items)
            {
                target.Remove(i);
            }
        }

        /// <summary>
        /// Removes a range of elements from the List.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="target">The target.</param>
        /// <param name="items">The items to remove.</param>
        public static void RemoveRange<T>(this ICollection<T> target, IEnumerable<T> items)
        {
            Contract.Requires(target != null);
            Contract.Requires(items != null);

            foreach (var i in items)
            {
                target.Remove(i);
            }
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the List.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="items">The collection whose elements should be added to the end of the List. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public static void AddRange(this IList target, IEnumerable items)
        {
            Contract.Requires(target != null);
            Contract.Requires(items != null);

            foreach (var i in items)
            {
                target.Add(i);
            }
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the List.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="target">The target.</param>
        /// <param name="items">The collection whose elements should be added to the end of the List. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> items)
        {
            Contract.Requires(target != null);
            Contract.Requires(items != null);

            foreach (var i in items)
            {
                target.Add(i);
            }
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the List, but ignores all <see cref="ArgumentException"/>, e.g. when trying to add duplicate keys to a dictionary.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="target">The target.</param>
        /// <param name="items">The collection whose elements should be added to the end of the List. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public static void TryAddRange<T>(this ICollection<T> target, IEnumerable<T> items)
        {
            Contract.Requires(target != null);
            Contract.Requires(items != null);

            foreach (var i in items)
            {
                try
                {
                    target.Add(i);
                }
                catch (ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Takes the specified number of items from the target. If target contains less items than specified, all available items are returned.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="target">The target.</param>
        /// <param name="numberOfItems">The number of items to take.</param>
        /// <returns>A list that contains up to n items.</returns>
        public static IList<T> Take<T>(this IEnumerator<T> target, int numberOfItems)
        {
            Contract.Requires(target != null);
            Contract.Requires(numberOfItems >= 0);
            Contract.Ensures(Contract.Result<IList<T>>() != null);

            var result = new List<T>(numberOfItems);

            while ((numberOfItems > 0) && (target.MoveNext()))
            {
                result.Add(target.Current);
                numberOfItems -= 1;
            }

            return result;
        }

        /// <summary>
        /// Get the value of the DescriptionAttribute associated with the given item.
        /// </summary>
        /// <param name="item">The item to lookup. This can be a MemberInfo like FieldInfo, PropertyInfo...</param>
        /// <returns>The associated description, or null if the item does not have a Description attribute.</returns>
        public static string TryGetDescription(this ICustomAttributeProvider item)
        {
            Contract.Requires(item != null);

            var attribute = item.GetCustomAttributes(typeof(DescriptionAttribute), false).OfType<DescriptionAttribute>().FirstOrDefault();

            return attribute != null ? attribute.Description : null;
        }

        /// <summary>
        /// Get the value of the DisplayNameAttribute associated with the given item.
        /// </summary>
        /// <param name="item">The item to lookup. This can be a MemberInfo like FieldInfo, PropertyInfo...</param>
        /// <returns>The associated display name, or null if the item does not have a DisplayName attribute.</returns>
        public static string TryGetDisplayName(this ICustomAttributeProvider item)
        {
            Contract.Requires(item != null);

            var attribute = item.GetCustomAttributes(typeof(DisplayNameAttribute), false).OfType<DisplayNameAttribute>().FirstOrDefault();

            return attribute != null ? attribute.DisplayName : null;
        }

        /// <summary>
        /// Get the value of the DescriptionAttribute associated with the given item.
        /// </summary>
        /// <param name="item">The item to lookup. This can be a MemberInfo like FieldInfo, PropertyInfo...</param>
        /// <returns>The associated description.</returns>
        /// <exception cref="ArgumentException">The item does not have a Description attribute.</exception>
        public static string GetDescription(this ICustomAttributeProvider item)
        {
            Contract.Requires(item != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var description = item.TryGetDescription();

            if (description == null)
                throw new ArgumentException(@"Item does not have a DescrtiptionAttribute: " + item);

            return description;
        }

        /// <summary>
        /// Returns a list of custom attributes identified by the type. <see cref="MemberInfo.GetCustomAttributes(Type, bool)"/>
        /// </summary>
        /// <typeparam name="T">The type of attributes to return.</typeparam>
        /// <param name="self">The member info of the object to evaluate.</param>
        /// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
        /// <returns>An array of custom attributes applied to this member, or an array with zero (0) elements if no attributes have been applied.</returns>
        /// <exception cref="System.TypeLoadException">A custom attribute type cannot be loaded</exception>
        /// <exception cref="System.InvalidOperationException">This member belongs to a type that is loaded into the reflection-only context. See How to: Load Assemblies into the Reflection-Only Context.</exception>
        public static IEnumerable<T> GetCustomAttributes<T>(this ICustomAttributeProvider self, bool inherit)
        {
            Contract.Requires(self != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            return self.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        /// <summary>
        /// Determines whether the assembly is located in the same folder or a sub folder of the specified program folder.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="programFolder">The program folder.</param>
        /// <returns>
        ///   <c>true</c> if the assembly is located in the same folder or a sub folder of the specified program folder; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAssemblyInSubfolderOf(this AssemblyName assemblyName, string programFolder)
        {
            Contract.Requires(assemblyName != null);
            Contract.Requires(programFolder != null);

            if (assemblyName.CodeBase == null)
                return false;

            var assemblyDirectory = Path.GetDirectoryName(new Uri(assemblyName.CodeBase).LocalPath);

            return assemblyDirectory.StartsWith(programFolder, StringComparison.OrdinalIgnoreCase);
        }

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
        /// A wrapper for <see cref="AppDomain.CreateInstanceAndUnwrap(string, string)"/>
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <param name="appDomain">The application domain.</param>
        /// <returns>The proxy of the unwrapped type.</returns>
        public static T CreateInstanceAndUnwrap<T>(this AppDomain appDomain) where T : class
        {
            Contract.Requires(appDomain != null);
            Contract.Ensures(Contract.Result<T>() != null);

            return (T)appDomain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName, typeof(T).FullName);
        }

        /// <summary>
        /// Helper method to debug LINQ method chains and trace every enumerated item. 
        /// This method is only active in DEBUG configuration, in RELEASE builds it will simply pass the items.
        /// </summary>
        /// <typeparam name="T">The type of objects to trace.</typeparam>
        /// <param name="items">The items to trace.</param>
        /// <param name="action">The action to be called for every item enumerated.</param>
        /// <returns>The items enumerator.</returns>
        public static IEnumerable<T> Trace<T>(this IEnumerable<T> items, Action<T> action)
        {
            Contract.Requires(items != null);
            Contract.Requires(action != null);

#if DEBUG
            foreach (var item in items)
            {
                action(item);
                yield return item;
            }
#else
            return items;
#endif
        }

        /// <summary>
        /// Rounds a double-precision floating-point value to a specified number of fractional digits.
        /// </summary>
        /// <returns>
        /// The number nearest to <paramref name="value"/> that contains a number of fractional digits equal to <paramref name="digits"/>.
        /// </returns>
        /// <param name="value">A double-precision floating-point number to be rounded.</param>
        /// <param name="digits">The number of fractional digits in the return value.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="digits"/> is less than 0 or greater than 15.</exception>
        public static double Round(this double value, int digits)
        {
            Contract.Requires((digits >= 0) && (digits <= 15));

            return Math.Round(value, digits);
        }

        /// <summary>
        /// Shortcut to test if any of the given characters are contained in the specified string.
        /// </summary>
        /// <param name="self">The string to analyze self.</param>
        /// <param name="characters">The characters to test for.</param>
        /// <returns></returns>
        public static bool ContainsAny(this string self, params char[] characters)
        {
            Contract.Requires(self != null);
            Contract.Requires(characters != null);

            return self.IndexOfAny(characters) >= 0;
        }

        /// <summary>
        /// Shortcut to test if any of the given items are contained in the specified object.
        /// </summary>
        /// <typeparam name="T">The type of objects.</typeparam>
        /// <param name="self">The object to analyze.</param>
        /// <param name="items">The items to test for.</param>
        /// <returns></returns>
        public static bool ContainsAny<T>(this IEnumerable<T> self, params T[] items)
        {
            Contract.Requires(self != null);
            Contract.Requires(items != null);

            return items.Any(self.Contains);
        }

        /// <summary>
        /// Shortcut to test if any of the given items are contained in the specified object.
        /// </summary>
        /// <typeparam name="T">The type of objects.</typeparam>
        /// <param name="self">The object to analyze.</param>
        /// <param name="items">The items to test for.</param>
        /// <param name="comparer">The comparer.</param>
        /// <returns></returns>
        public static bool ContainsAny<T>(this IEnumerable<T> self, IEqualityComparer<T> comparer, params T[] items)
        {
            Contract.Requires(self != null);
            Contract.Requires(items != null);

            return items.Any(item => self.Contains(item, comparer));
        }

        /// <summary>
        /// Ensures the specified string is properly quoted if it contains spaces or one kind of quotes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The quoted value if the value needs to be quoted, otherwise the original value.
        /// </returns>
        /// <exception cref="System.ArgumentException">The string already contains both single and double quotes</exception>
        public static string QuoteSpaces(this string value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            value = value.Trim();

            if (value.ContainsAny(' ', '\'', '"'))
            {
                if (value.Contains('\''))
                {
                    if (value.Contains('"'))
                        throw new ArgumentException("The string already contains both single and double quotes: " + value);

                    return "\"" + value + "\"";
                }
                else
                {
                    return "'" + value + "'";
                }
            }

            return value;
        }

        /// <summary>
        /// Transposes the specified items, i.e. exchanges key and value.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="items">The items.</param>
        /// <returns>The transposed items.</returns>
        public static IEnumerable<KeyValuePair<TValue, TKey>> Transpose<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Contract.Requires(items != null);
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<TValue, TKey>>>() != null);

            return items.Select(item => new KeyValuePair<TValue, TKey>(item.Value, item.Key));
        }

        /// <summary>
        /// Gets the value from the dictionary, or the default value if no item with the specified key exists.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// The value from the dictionary, or the default value if no item with the specified key exists.
        /// </returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            Contract.Requires(dictionary != null);

            TValue value;

            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the value from the dictionary, or the default value of <typeparamref name="TValue"/> if no item with the specified key exists.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key to lookup.</param>
        /// <returns>
        /// The value from the dictionary, or the default value of <typeparamref name="TValue"/> if no item with the specified key exists.
        /// </returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            Contract.Requires(dictionary != null);

            return GetValueOrDefault(dictionary, key, default(TValue));
        }

        /// <summary>
        /// Converts the value of the current <see cref="T:System.DateTime"/> object to its equivalent short date and time string representation.
        /// </summary>
        /// <returns>
        /// A string that contains both the short date string and short time string representation, delimited with a space.
        /// </returns>
        public static string ToShortDateTimeString(this DateTime value)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return value.ToShortDateString() + " " + value.ToShortTimeString();
        }

        /// <summary>
        /// Converts the value of the current <see cref="T:System.DateTime" /> object to its equivalent date and time string representation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="dateTimeDelimiter">The delimiter between date and time.</param>
        /// <returns>
        /// A string that contains both the short date string and time string representation.
        /// </returns>
        /// <remarks>
        /// The format is "yyyy-MM-dd" + delimiter + "HH:mm:ss"
        /// </remarks>
        public static string ToInvariantDateTimeString(this DateTime value, string dateTimeDelimiter)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + dateTimeDelimiter + value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the value of the current <see cref="T:System.DateTime" /> object to its equivalent date and time string representation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A string that contains both the short date string and time string representation, delimited with a space.
        /// </returns>
        /// <remarks>
        /// The format is "yyyy-MM-dd HH:mm:ss"
        /// </remarks>
        public static string ToInvariantDateTimeString(this DateTime value)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return value.ToInvariantDateTimeString(" ");
        }

        /// <summary>
        /// Shortcut to <see cref="System.Windows.Threading.Dispatcher.BeginInvoke(Delegate, Object[])"/>
        /// </summary>
        public static void BeginInvoke(this DispatcherObject self, Action action)
        {
            Contract.Requires(self != null);
            Contract.Requires(action != null);

            self.Dispatcher.BeginInvoke(action);
        }

        /// <summary>
        /// Shortcut to <see cref="System.Windows.Threading.Dispatcher.BeginInvoke(DispatcherPriority, Delegate)"/>
        /// </summary>
        public static void BeginInvoke(this DispatcherObject self, DispatcherPriority priority, Action action)
        {
            Contract.Requires(self != null);
            Contract.Requires(action != null);

            self.Dispatcher.BeginInvoke(priority, action);
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
            where T: Behavior, new()
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
    }
}
