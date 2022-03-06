namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    internal static class XlfNames
    {
        public static XNamespace XsiNS = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace XliffNS = "urn:oasis:names:tc:xliff:document:1.2";
        public static readonly XName Xliff = XliffNS + "xliff";
        public static readonly XName FileElement = XliffNS + "file";
        public static readonly XName BodyElement = XliffNS + "body";
        public static readonly XName GroupElement = XliffNS + "group";
        public static readonly XName TransUnitElement = XliffNS + "trans-unit";
        public static readonly XName SourceElement = XliffNS + "source";
        public static readonly XName TargetElement = XliffNS + "target";
        public static readonly XName NoteElement = XliffNS + "note";

        public const string IdAttribute = "id";
        public const string DataTypeAttribute = "datatype";
        public const string OriginalAttribute = "original";
        public const string SourceLanguageAttribute = "source-language";
        public const string TargetLanguageAttribute = "target-language";
        public const string StateAttribute = "state";
        public const string FromAttribute = "from";
        public const string TranslateAttribute = "translate";

        public const string NewState = "new";
        public static readonly HashSet<string> ApprovedStates = new(new[] { "translated", "final", "signed-off" });
        public static readonly HashSet<string> NeedsReviewStates = new(new[] { "needs-review-translation", "needs-translation", "needs-review-l10n", "needs-l10n", "needs-review-adaptation" });

        public const string FromResx = "MultilingualBuild";
        public const string FromResxSpecific = "resx";
    }
}