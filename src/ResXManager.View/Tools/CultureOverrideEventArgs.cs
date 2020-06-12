namespace ResXManager.View.Tools
{
    using System;
    using System.Globalization;

    using JetBrains.Annotations;

    public class CultureOverrideEventArgs : EventArgs
    {
        public CultureOverrideEventArgs([NotNull] CultureInfo neutralCulture, CultureInfo? specificCulture)
        {
            SpecificCulture = specificCulture;
            NeutralCulture = neutralCulture;
        }

        [NotNull]
        public CultureInfo NeutralCulture { get; }

        public CultureInfo? SpecificCulture { get; }
    }
}
