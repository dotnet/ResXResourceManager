namespace ResXManager.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    internal class ResourceScope : IResourceScope
    {
        public ResourceScope([NotNull] object entries, [NotNull] object languages, [NotNull] object comments)
        {
            Entries = CastResourceTableEntries(entries);
            Languages = CastLanguages(languages);
            Comments = CastLanguages(comments);
        }

        public IEnumerable<ResourceTableEntry> Entries { get; }

        public IEnumerable<CultureKey> Languages { get; }

        public IEnumerable<CultureKey> Comments { get; }

        private static IEnumerable<CultureKey> CastLanguages(object languages)
        {
            var obj = languages.PsObjectCast<object>();

            switch (obj)
            {
                case string str:
                    return new[] { CultureKey.Parse(str) };

                case IEnumerable enumerable:
                    return enumerable.PsCast<object>().Select(CultureKey.Parse).ToArray();

                default:
                    return new[] { CultureKey.Parse(obj.PsObjectCast<object>()) };
            }
        }

        private static IEnumerable<ResourceTableEntry> CastResourceTableEntries(object entries)
        {
            var obj = entries.PsObjectCast<object>();

            switch (obj)
            {
                case IEnumerable enumerable:
                    return enumerable.PsCast<ResourceTableEntry>().ToArray();

                default:
                    return new[] { obj.PsObjectCast<ResourceTableEntry>() };
            }
        }
    }

    internal static class ExtensionMethods
    {
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> PsCast<T>([NotNull][ItemNotNull] this IEnumerable items)
        {
            return items.OfType<object>().Select(PsObjectCast<T>);
        }

        [NotNull]
        public static T PsObjectCast<T>([NotNull] this object item)
        {
            var type = item.GetType();

            var value = type.Name == "PSObject" ? type.GetProperty("BaseObject")?.GetValue(item) : item;

            if (value == null)
                throw new InvalidOperationException("Unable to cast PowerShell object.");

            return (T)value;
        }
    }
}