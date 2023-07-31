namespace ResXManager.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    internal sealed class ResourceScope : IResourceScope
    {
        public ResourceScope(object entries, object languages, object comments)
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
        public static IEnumerable<T> PsCast<T>(this IEnumerable items)
        {
            return items.OfType<object>().Select(PsObjectCast<T>);
        }

        public static T PsObjectCast<T>(this object item)
        {
            var type = item.GetType();

            var value = type.Name == "PSObject" ? type.GetProperty("BaseObject")?.GetValue(item) : item;

            if (value == null)
                throw new InvalidOperationException("Unable to cast PowerShell object.");

            return (T)value;
        }
    }
}