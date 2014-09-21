namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public class CultureOverrideEventArgs : EventArgs
    {
        public CultureOverrideEventArgs(CultureInfo neutralCulture, CultureInfo specificCulture)
        {
            Contract.Requires(neutralCulture != null);
            Contract.Requires(specificCulture != null);

            SpecificCulture = specificCulture;
            NeutralCulture = neutralCulture;
        }

        public CultureInfo NeutralCulture
        {
            get;
            private set;
        }

        public CultureInfo SpecificCulture
        {
            get;
            private set;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(NeutralCulture != null);
            Contract.Invariant(SpecificCulture != null);
        }
    }
}
