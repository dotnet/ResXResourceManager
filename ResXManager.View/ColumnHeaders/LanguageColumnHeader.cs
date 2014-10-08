namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    public abstract class LanguageColumnHeader : ObservableObject, ILanguageColumnHeader
    {
        private readonly CultureKey _cultureKey;

        protected LanguageColumnHeader(CultureKey cultureKey)
        {
            Contract.Requires(cultureKey != null);

            _cultureKey = cultureKey;

            NeutralCultureCountyOverrides.Default.OverrideChanged += NeutralCultureCountyOverrides_OverrideChanged;
        }

        void NeutralCultureCountyOverrides_OverrideChanged(object sender, CultureOverrideEventArgs e)
        {
            if (e.NeutralCulture.Equals(_cultureKey.Culture))
            {
                OnPropertyChanged(() => CultureKey);
            }
        }

        public CultureKey CultureKey
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureKey>() != null);

                return _cultureKey;
            }
        }

        public abstract ColumnType ColumnType
        {
            get;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_cultureKey != null);
        }
    }
}
