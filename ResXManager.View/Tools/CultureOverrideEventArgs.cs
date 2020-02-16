namespace ResXManager.View.Tools
{
    using System;
    using System.Globalization;

    using JetBrains.Annotations;

    public class CultureOverrideEventArgs : EventArgs
    {
        public CultureOverrideEventArgs([NotNull] CultureInfo neutralCulture, [CanBeNull] CultureInfo specificCulture)
        {
            SpecificCulture = specificCulture;
            NeutralCulture = neutralCulture;
        }

        [NotNull]
        public CultureInfo NeutralCulture { get; }

        [CanBeNull]
        public CultureInfo SpecificCulture { get; }
    }
}
