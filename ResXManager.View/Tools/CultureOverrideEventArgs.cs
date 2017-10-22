namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    public class CultureOverrideEventArgs : EventArgs
    {
        public CultureOverrideEventArgs([NotNull] CultureInfo neutralCulture, [CanBeNull] CultureInfo specificCulture)
        {
            Contract.Requires(neutralCulture != null);
            Contract.Requires(specificCulture != null);

            SpecificCulture = specificCulture;
            NeutralCulture = neutralCulture;
        }

        [NotNull]
        public CultureInfo NeutralCulture { get; }

        [CanBeNull]
        public CultureInfo SpecificCulture { get; }
    }
}
