namespace ResXManager.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;

    internal class ResourceScope : IResourceScope
    {
        public ResourceScope([NotNull] object entries, [NotNull] object languages, [NotNull] object comments)
        {
            Contract.Requires(entries != null);
            Contract.Requires(languages != null);
            Contract.Requires(comments != null);

            // ReSharper disable AssignNullToNotNullAttribute
            Entries = entries.PsObjectCast<object>().TryCast().Returning<IEnumerable<ResourceTableEntry>>()
                .When<IEnumerable>(item => item.PsCast<ResourceTableEntry>().ToArray())
                .When<object>(item => new[] { item.PsObjectCast<ResourceTableEntry>() })
                .Result;

            if (Entries == null)
                throw new ArgumentException("Invalid input", nameof(entries));

            Languages = languages.PsObjectCast<object>().TryCast().Returning<IEnumerable<CultureKey>>()
                .When<string>(item => new[] { CultureKey.Parse(item) })
                .When<IEnumerable>(item => item.PsCast<object>().Select(CultureKey.Parse).ToArray())
                .When<object>(item => new[] { CultureKey.Parse(item.PsObjectCast<object>()) })
                .Result;

            if (Languages == null)
                throw new ArgumentException("Invalid input", nameof(languages));

            Comments = comments.PsObjectCast<object>().TryCast().Returning<IEnumerable<CultureKey>>()
                .When<string>(item => new[] { CultureKey.Parse(item) })
                .When<IEnumerable>(item => item.PsCast<object>().Select(CultureKey.Parse).ToArray())
                .When<object>(item => new[] { CultureKey.Parse(item.PsObjectCast<object>()) })
                .Result;
            // ReSharper restore AssignNullToNotNullAttribute

            if (Comments == null)
                throw new ArgumentException("Invalid input", nameof(comments));
        }

        public IEnumerable<ResourceTableEntry> Entries { get; }

        public IEnumerable<CultureKey> Languages { get; }

        public IEnumerable<CultureKey> Comments { get; }
    }

    internal static class ExtensionMethods
    {
        [NotNull]
        public static IEnumerable<T> PsCast<T>([NotNull] this IEnumerable items)
        {
            Contract.Requires(items != null);

            return items.OfType<object>().Select(PsObjectCast<T>);
        }

        [NotNull]
        public static T PsObjectCast<T>([NotNull] this object item)
        {
            Contract.Requires(item != null);

            var type = item.GetType();

            var value = type.Name == "PSObject" ? type.GetProperty("BaseObject")?.GetValue(item) : item;

            if (value == null)
                throw new InvalidOperationException("Unable to cast PowerShell object.");

            return (T)value;
        }
    }
}