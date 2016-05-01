namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using TomsToolbox.Desktop;

    public class EntryChange : ObservableObject
    {
        private string _text;

        public EntryChange(ResourceTableEntry entry, string text, CultureInfo culture, ColumnKind columnKind, string originalText)
        {
            Contract.Requires(entry != null);
            Entry = entry;
            Text = ProposedText = text;
            Culture = culture;
            ColumnKind = columnKind;
            OriginalText = originalText;
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                SetProperty(ref _text, value, () => Text);
            }
        }

        public ResourceTableEntry Entry { get; private set; }

        public string ProposedText { get; private set; }

        public CultureInfo Culture { get; private set; }

        public ColumnKind ColumnKind { get; private set; }

        public string OriginalText { get; private set; }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Entry != null);
        }
    }
}