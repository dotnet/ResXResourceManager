namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public class CultureOverrideEventArgs : EventArgs
    {
        private readonly CultureInfo _neutralCulture;
        private readonly CultureInfo _specificCulture;

        public CultureOverrideEventArgs(CultureInfo neutralCulture, CultureInfo specificCulture)
        {
            Contract.Requires(neutralCulture != null);
            Contract.Requires(specificCulture != null);

            _specificCulture = specificCulture;
            _neutralCulture = neutralCulture;
        }

        public CultureInfo NeutralCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return _neutralCulture;
            }
        }

        public CultureInfo SpecificCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return _specificCulture;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_neutralCulture != null);
            Contract.Invariant(_specificCulture != null);
        }
    }
}
