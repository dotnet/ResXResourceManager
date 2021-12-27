namespace ResXManager.Model
{
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

        public const string NewState = "new";
        public const string TranslatedState = "translated";
        public const string NeedsReviewState = "needs-review-translation";
    }
}