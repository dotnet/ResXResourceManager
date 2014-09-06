namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public class CodeReference
    {
        private static Thread _backgroundThread;

        private CodeReference(ProjectFile projectFile, int lineNumber, string line, int[] baseNameIndexes, int baseNameLength, int[] keyNameIndexes, int keyNameLength)
        {
            Contract.Requires(projectFile != null);
            Contract.Requires(line != null);
            Contract.Requires(baseNameIndexes != null);
            Contract.Requires(keyNameIndexes != null);

            ProjectFile = projectFile;
            LineNumber = lineNumber;

            var baseNameRanges = baseNameIndexes.Select(index => new { Start = index, End = index + baseNameLength });
            var keyNameRanges = keyNameIndexes.Select(index => new { Start = index, End = index + keyNameLength });

            var combinations = baseNameRanges.SelectMany(
                baseNameRange => keyNameRanges.Select(
                    keyNameRange => new
                    {
                        BaseNameRange = baseNameRange,
                        KeyNameRange = keyNameRange,
                        Distance = Math.Min(Math.Abs(baseNameRange.End - keyNameRange.Start), Math.Abs(baseNameRange.Start - keyNameRange.End))
                    }));

            var match = combinations.OrderBy(item => item.Distance).First();

            if (match.BaseNameRange.Start < match.KeyNameRange.Start)
            {
                LineSegments = line.GetSegments(match.BaseNameRange.Start, match.BaseNameRange.End, match.KeyNameRange.Start, match.KeyNameRange.End);
                IsValid = IsValidClassValueDeclaration(LineSegments.GetTrimmedSegment(0), LineSegments.GetTrimmedSegment(2), LineSegments.GetTrimmedSegment(4));

            }
            else
            {
                LineSegments = line.GetSegments(match.KeyNameRange.Start, match.KeyNameRange.End, match.BaseNameRange.Start, match.BaseNameRange.End);
                IsValid = IsValidValueClassDeclaration(LineSegments.GetTrimmedSegment(0), LineSegments.GetTrimmedSegment(2), LineSegments.GetTrimmedSegment(4));
            }
        }

        public int LineNumber { get; private set; }
        public ProjectFile ProjectFile { get; private set; }
        public IList<string> LineSegments { get; private set; }
        private bool IsValid
        {
            get;
            set;
        }

        public static void StopFind()
        {
            if ((_backgroundThread == null) || (_backgroundThread.ThreadState != ThreadState.Background))
                return;

            _backgroundThread.Abort();
            _backgroundThread = null;
        }

        public static void BeginFind(IEnumerable<ResourceEntity> resourceEntities, IEnumerable<ProjectFile> allSourceFiles)
        {
            Contract.Requires(resourceEntities != null);
            Contract.Requires(allSourceFiles != null);

            StopFind();

            var sourceFiles = allSourceFiles.Where(item => !item.IsResourceFile() && !item.IsDesignerFile()).ToArray();
            var resourceTableEntries = resourceEntities.SelectMany(entity => entity.Entries).ToArray();

            _backgroundThread = new Thread(() => FindCodeReferences(sourceFiles, resourceTableEntries)) { IsBackground = true, Priority = ThreadPriority.Lowest };
            _backgroundThread.Start();
        }

        public static void FindCodeReferences(IEnumerable<ProjectFile> projectFiles, IList<ResourceTableEntry> resourceTableEntries)
        {
            Contract.Requires(projectFiles != null);
            Contract.Requires(resourceTableEntries != null);

            try
            {
                foreach (var entry in resourceTableEntries)
                {
                    entry.CodeReferences = null;
                }

                var sourceFiles = projectFiles.Select(file => new FileInfo(file, file.ReadAllLines())).ToArray();
                var entriesByBaseName = resourceTableEntries.GroupBy(entry => entry.Owner.BaseName);

                foreach (var entryiesGroup in entriesByBaseName.AsParallel())
                {
                    var baseName = entryiesGroup.Key;
                    var tableEntries = entryiesGroup.ToArray();

                    foreach (var sourceFile in sourceFiles)
                    {
                        Contract.Assume(sourceFile != null);
                        FindCodeReferences(sourceFile, baseName, tableEntries);
                    }
                }

                foreach (var sourceFile in sourceFiles.Where(file => file.FileKind != FileKind.Undefined))
                {
                    Contract.Assume(sourceFile != null);
                    FindCodeReferences(sourceFile, @"StringResourceKey", resourceTableEntries);
                }

                foreach (var entry in resourceTableEntries.Where(entry => entry.CodeReferences == null))
                {
                    entry.CodeReferences = new CodeReference[0];
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private static void FindCodeReferences(FileInfo source, string baseName, IList<ResourceTableEntry> entries)
        {
            Contract.Requires(source != null);
            Contract.Requires(baseName != null);
            Contract.Requires(entries != null);

            var lineNumber = 1;

            var projectFile = source.ProjectFile;

            var fileKind = source.FileKind;

            var stringComparison = (fileKind == FileKind.VisualBasic) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var line in source.Lines)
            {
                Contract.Assume(line != null);

                if (line.IsCommentLine(fileKind))
                    continue;

                var baseNameIndexes = line.IndexesOfWords(baseName, stringComparison).ToArray();
                if (baseNameIndexes.Length > 0)
                {
                    foreach (var entry in entries)
                    {
                        var keyNameIndexes = line.IndexesOfWords(entry.Key, stringComparison).ToArray();
                        if (keyNameIndexes.Length > 0)
                        {
                            var codeReference = new CodeReference(projectFile, lineNumber, line, baseNameIndexes, baseName.Length, keyNameIndexes, entry.Key.Length);

                            if (!codeReference.IsValid)
                                continue;

                            var codeReferences = entry.CodeReferences ?? (entry.CodeReferences = new ObservableCollection<CodeReference>());
                            codeReferences.Add(codeReference);
                        }
                    }
                }

                lineNumber += 1;
            }
        }

        /// <summary>
        /// Determines whether this is a valid declaration where the position of the class name is before the key.
        /// </summary>
        /// <param name="left">The left text segment.</param>
        /// <param name="middle">The middle text segment.</param>
        /// <param name="right">The right text segment.</param>
        /// <returns>
        /// True if this is a valid declaration where the position of the class name is before the key.
        /// </returns>
        /// <remarks>
        /// C#, VB, cshtml: "Resources.Key" or "Properties.Resources.Key" but not: "Resources.Key.Something" or "Resources.Something.Key" (could be e.g. some namespace declaration)<br/>
        /// XAML: {x:Static properties:Resources.Key}"
        /// <p/>
        /// C++: like C#, but -> instead of .
        /// <p/>
        /// ASP: &lt;%$ Resources: Class, Key %&gt;
        /// </remarks>
        private static bool IsValidClassValueDeclaration(string left, string middle, string right)
        {
            Contract.Requires(left != null);
            Contract.Requires(middle != null);
            Contract.Requires(right != null);

            // C#, VB, cshtml: "Resources.Key" or "Properties.Resources.Key", but not: "Resources.Key.Something" or "Resources.Something.Key" (could be e.g. some namespace declaration)
            // XAML: {x:Static properties:Resources.Key}" IsCheckable="True"
            if ((middle == ".") && !right.StartsWith(".", StringComparison.Ordinal))
                return true;

            // C++: like C#, but :: instead of .
            if ((middle == "::") && !right.StartsWith("::", StringComparison.Ordinal) && !right.StartsWith("->", StringComparison.Ordinal))
                return true;

            // ASP: <%$ Resources: Class, Key %>
            if (left.EndsWith("Resources:", StringComparison.Ordinal) && (middle == ",") && right.StartsWith("%>", StringComparison.Ordinal))
                return true;

            // C#, VB (indirect): var str = Properties.Resources.ResourceManager.GetString("Key");
            if (right.StartsWith("\"", StringComparison.Ordinal) && middle.DropSpaces().Equals(".ResourceManager.GetString(\""))
                return true;

            return IsValidStringReference(left, middle, middle, right);
        }

        /// <summary>
        /// Determines whether this is a valid declaration where the position of the key name is before the class.
        /// </summary>
        /// <param name="left">The left text segment.</param>
        /// <param name="middle">The middle text segemnt.</param>
        /// <param name="right">The right text segment.</param>
        /// <returns>
        /// True if this is a valid declaration where position of the key name is before the class.
        /// </returns>
        private static bool IsValidValueClassDeclaration(string left, string middle, string right)
        {
            Contract.Requires(left != null);
            Contract.Requires(middle != null);
            Contract.Requires(right != null);

            return IsValidStringReference(middle, right, left, middle);
        }

        /// <summary>
        /// Determines whether this is a reference where the key is specified as string.
        /// </summary>
        /// <param name="beforeClass">The text before the class name.</param>
        /// <param name="afterClass">The text after the class name.</param>
        /// <param name="beforeKey">The text before the key.</param>
        /// <param name="afterKey">The text after the key.</param>
        /// <returns>
        /// <c>true</c> if this is a reference where the key is specified as string; otherwise false.
        /// </returns>
        /// <remarks>
        /// In attribute, e.g. [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Key")]
        /// <p/>
        /// In calls, e.g. MyClass.LookupResource("Key", typeof(Resources))
        /// or MyClass.LookupResource("Key", typeof(Properties.Resources))
        /// <p/>
        /// In generic calls e.g. MyClass.LookupResource&lt;Resources&gt;("Key")
        /// </remarks>
        private static bool IsValidStringReference(string beforeClass, string afterClass, string beforeKey, string afterKey)
        {
            Contract.Requires(beforeClass != null);
            Contract.Requires(afterClass != null);
            Contract.Requires(beforeKey != null);
            Contract.Requires(afterKey != null);

            // Key is specified as string
            if (!beforeKey.EndsWith("\"", StringComparison.Ordinal) || !afterKey.StartsWith("\"", StringComparison.Ordinal))
                return false;

            var beforeNoNamespace = beforeClass.DropNamespacePath();

            // class is in a typeof() statement.
            if (afterClass.StartsWith(")", StringComparison.Ordinal))
            {
                if (beforeNoNamespace.EndsWith("typeof(", StringComparison.Ordinal) || beforeNoNamespace.EndsWith("GetType(", StringComparison.OrdinalIgnoreCase))
                    return true;
            }


            // class is in a generic parameter.
            return beforeNoNamespace.EndsWith("<", StringComparison.Ordinal) && afterClass.StartsWith(">", StringComparison.Ordinal);
        }

        class FileInfo
        {
            public FileInfo(ProjectFile projectFile, string[] lines)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(lines != null);

                ProjectFile = projectFile;
                Lines = lines;
                FileKind = projectFile.GetFileKind();
            }

            public FileKind FileKind
            {
                get;
                private set;
            }

            public ProjectFile ProjectFile
            {
                get;
                private set;
            }

            public string[] Lines
            {
                get;
                private set;
            }

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(ProjectFile != null);
                Contract.Invariant(Lines != null);
            }

        }
    }

    enum FileKind
    {
        Undefined,
        VisualBasic,
        CSharp,
    }

    static class CodeReferenceExtensionMethods
    {
        public static string GetTrimmedSegment(this IList<string> segments, int index)
        {
            Contract.Requires(segments != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if ((index < 0) || (index >= segments.Count))
                return String.Empty;

            return (segments[index] ?? String.Empty).Trim();
        }

        [ContractVerification(false)]
        public static IList<string> GetSegments(this string line, int x0, int x1, int x2, int x3)
        {
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            Contract.Ensures(Contract.Result<IList<string>>().Count == 5);
            Contract.Ensures(Contract.Result<IList<string>>().All(x => x != null));


            return new[]
            {
                line.Substring(0, x0).TrimStart(),
                line.Substring(x0, x1 - x0),
                line.Substring(x1, x2 - x1),
                line.Substring(x2, x3 - x2),
                line.Substring(x3).TrimEnd()
            };
        }

        public static IEnumerable<int> IndexesOfWords(this string line, string word, StringComparison stringComparison)
        {
            Contract.Requires(line != null);
            Contract.Requires(word != null);
            Contract.Ensures(Contract.Result<IEnumerable<int>>() != null);

            var startIndex = 0;

            while (true)
            {
                startIndex = line.IndexOf(word, startIndex, stringComparison);

                if (startIndex < 0)
                    yield break;

                var endIndex = startIndex + word.Length;

                if ((startIndex <= 0) || IsNonWordChar(line[startIndex - 1]))
                {
                    if ((endIndex >= line.Length) || IsNonWordChar(line[endIndex]))
                    {
                        yield return startIndex;
                    }
                }

                startIndex = endIndex;
            }
        }

        public static bool IsNonWordChar(this char c)
        {
            return !IsWordChar(c);
        }

        private static bool IsWordChar(this char c)
        {
            return (Char.IsLetter(c) || Char.IsDigit(c) || (c == '_'));
        }

        public static string DropSpaces(this string value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return value.Any(Char.IsWhiteSpace) ? new string(value.Where(c => !Char.IsWhiteSpace(c)).ToArray()) : value;
        }

        public static string DropNamespacePath(this string value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (value.LastOrDefault() != '.')
                return value;

            return new string(value.Reverse().SkipWhile(c => c.IsWordChar() || c == '.').SkipWhile(Char.IsWhiteSpace).Reverse().ToArray());

        }

        public static string[] ReadAllLines(this ProjectFile file)
        {
            Contract.Requires(file != null);
            try
            {
                Thread.Sleep(1);
                return File.ReadAllLines(file.FilePath);
            }
            catch
            {
            }

            return new string[0];
        }

        public static FileKind GetFileKind(this ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            if (projectFile.IsVisualBasicFile())
                return FileKind.VisualBasic;

            if (projectFile.IsCSharpFile())
                return FileKind.CSharp;

            return FileKind.Undefined;
        }

        public static bool IsCommentLine(this string line, FileKind fileKind)
        {
            Contract.Requires(line != null);

            if (fileKind == FileKind.Undefined)
                return false;

            line = line.TrimStart();

            switch (fileKind)
            {
                case FileKind.VisualBasic:
                    return line.StartsWith("'", StringComparison.Ordinal) || line.StartsWith("REM", StringComparison.Ordinal);

                case FileKind.CSharp:
                    return line.StartsWith("//", StringComparison.Ordinal);

                default:
                    return false;
            }
        }
    }
}
