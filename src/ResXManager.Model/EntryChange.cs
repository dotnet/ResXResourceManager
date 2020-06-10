namespace ResXManager.Model
{
    using System.Globalization;

    using JetBrains.Annotations;

    public class EntryChange
    {
        public EntryChange([NotNull] ResourceTableEntry entry, string? text, CultureInfo? culture, ColumnKind columnKind, string? originalText)
        {
            Entry = entry;
            Text = text;
            Culture = culture;
            ColumnKind = columnKind;
            OriginalText = originalText;
        }

        [NotNull]
        public ResourceTableEntry Entry { get; }

        public string? Text { get; }

        public CultureInfo? Culture { get; }

        public ColumnKind ColumnKind { get; }

        public string? OriginalText { get; }
    }
}