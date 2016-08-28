namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using TomsToolbox.Desktop;

    public class EntryChange : ObservableObject
    {
        private string _text;
        private readonly ResourceTableEntry _entry;

        public EntryChange(ResourceTableEntry entry, string text, CultureInfo culture, ColumnKind columnKind, string originalText)
        {
            Contract.Requires(entry != null);

            _entry = entry;

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

        public ResourceTableEntry Entry
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceTableEntry>() != null);
                return _entry;
            }
        }

        public string ProposedText { get; private set; }

        public CultureInfo Culture { get; private set; }

        public ColumnKind ColumnKind { get; private set; }

        public string OriginalText { get; private set; }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_entry != null);
        }
    }
}