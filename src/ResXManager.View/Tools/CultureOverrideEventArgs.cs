namespace ResXManager.View.Tools
{
    using System;
    using System.Globalization;

    public class CultureOverrideEventArgs : EventArgs
    {
        public CultureOverrideEventArgs(CultureInfo neutralCulture, CultureInfo? specificCulture)
        {
            SpecificCulture = specificCulture;
            NeutralCulture = neutralCulture;
        }

        public CultureInfo NeutralCulture { get; }

        public CultureInfo? SpecificCulture { get; }
    }
}
