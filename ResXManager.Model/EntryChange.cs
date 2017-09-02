namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    public class EntryChange
    {
        public EntryChange([NotNull] ResourceTableEntry entry, string text, CultureInfo culture, ColumnKind columnKind, string originalText)
        {
            Contract.Requires(entry != null);

            Entry = entry;
            Text = text;
            Culture = culture;
            ColumnKind = columnKind;
            OriginalText = originalText;
        }

        [NotNull]
        public ResourceTableEntry Entry { get; }

        public string Text { get; }

        public CultureInfo Culture { get; }

        public ColumnKind ColumnKind { get; }

        public string OriginalText { get; }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Entry != null);
        }
    }
}