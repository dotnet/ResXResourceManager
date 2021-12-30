namespace ResXManager.Model
{
    using System.Collections.Generic;

    public class CodeReference
    {
        public CodeReference(ProjectFile projectFile, int lineNumber, IList<string> lineSegments)
        {
            ProjectFile = projectFile;
            LineNumber = lineNumber;
            LineSegments = lineSegments;
        }

        public int LineNumber { get; }

        public ProjectFile? ProjectFile { get; }

        public IList<string>? LineSegments { get; }
    }
}