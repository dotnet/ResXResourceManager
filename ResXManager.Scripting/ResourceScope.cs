namespace ResXManager.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

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
            IEnumerable<CultureKey> resourceLanguages = null;

            switch (languages.PsObjectCast<object>())
            {
                case string str:
                    resourceLanguages = new[] {CultureKey.Parse(str)};
                    break;
                case IEnumerable enumerable:
                    resourceLanguages = enumerable.PsCast<object>().Select(CultureKey.Parse).ToArray();
                    break;
                case object obj:
                    resourceLanguages = new[] {CultureKey.Parse(obj.PsObjectCast<object>())};
                    break;
            }

            return resourceLanguages;
        }

        private static IEnumerable<ResourceTableEntry> CastResourceTableEntries(object entries)
        {
            IEnumerable<ResourceTableEntry> resourceTableEntries = null;

            switch (entries.PsObjectCast<object>())
            {
                case IEnumerable enumerable:
                    resourceTableEntries = enumerable.PsCast<ResourceTableEntry>().ToArray();
                    break;
                case object obj:
                    resourceTableEntries = new[] {obj.PsObjectCast<ResourceTableEntry>()};
                    break;
            }

            return resourceTableEntries;
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