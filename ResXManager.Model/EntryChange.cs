namespace tomenglertde.ResXManager.Model
{
    using System.Globalization;

    using JetBrains.Annotations;

    public class EntryChange
    {
        public EntryChange([NotNull] ResourceTableEntry entry, [CanBeNull] string text, [CanBeNull] CultureInfo culture, ColumnKind columnKind, [CanBeNull] string originalText)
        {
            Entry = entry;
            Text = text;
            Culture = culture;
            ColumnKind = columnKind;
            OriginalText = originalText;
        }

        [NotNull]
        public ResourceTableEntry Entry { get; }

        [CanBeNull]
        public string Text { get; }

        [CanBeNull]
        public CultureInfo Culture { get; }

        public ColumnKind ColumnKind { get; }

        [CanBeNull]
        public string OriginalText { get; }
    }
}