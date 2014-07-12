namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public class CodeReference
    {
        private static Thread _backgroundThread;

        public CodeReference(ProjectFile projectFile, IList<string> lineSegments, int lineNumber)
        {
            LineSegments = lineSegments;
            ProjectFile = projectFile;
            LineNumber = lineNumber;
        }

        public int LineNumber { get; private set; }
        public ProjectFile ProjectFile { get; private set; }
        public IList<string> LineSegments { get; private set; }

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

        private static void FindCodeReferences(IEnumerable<ProjectFile> sourceFiles, IList<ResourceTableEntry> resourceTableEntries)
        {
            Contract.Requires(sourceFiles != null);
            Contract.Requires(resourceTableEntries != null);

            try
            {
                foreach (var entry in resourceTableEntries)
                {
                    entry.CodeReferences.Clear();
                }

                var sourceFilesContent = sourceFiles.Select(file => new SourceFile { File=file, Lines=ReadAllLines(file)}).ToArray();
                var entriesByBaseName = resourceTableEntries.GroupBy(entry => entry.Owner.BaseName);

                foreach (var baseNameGroup in entriesByBaseName.AsParallel())
                {
                    var baseName = baseNameGroup.Key;

                    foreach (var sourceFile in sourceFilesContent)
                    {
                        var lineNumber = 1;

                        foreach (var line in sourceFile.Lines)
                        {
                            var baseNameIndexes = IndexesOfWords(line, baseName).ToArray();
                            if (baseNameIndexes.Length > 0)
                            {
                                foreach (var entry in baseNameGroup)
                                {
                                    var keyNameIndexes = IndexesOfWords(line, entry.Key).ToArray();
                                    if (keyNameIndexes.Length > 0)
                                    {
                                        entry.CodeReferences.Add(new CodeReference(sourceFile.File, GetLineSegments(line, baseNameIndexes, baseName.Length, keyNameIndexes, entry.Key.Length), lineNumber));
                                    }
                                }
                            }

                            lineNumber += 1;
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private static IList<string> GetLineSegments(string line, IEnumerable<int> firstIndexes, int firstLength, IEnumerable<int> secondIndexes, int secondLength)
        {
            Contract.Requires(firstIndexes != null);
            Contract.Requires(secondIndexes != null);

            var firstPoints = firstIndexes.Select(index => new {Start = index, End = index + firstLength});
            var secondPoints = secondIndexes.Select(index => new { Start = index, End = index + secondLength });

            var combinations = firstPoints.SelectMany(
                firstPoint => secondPoints.Select(
                    secondPoint => new
                    {
                        First = firstPoint,
                        Second = secondPoint,
                        Distance = Math.Min(Math.Abs(firstPoint.End - secondPoint.Start), Math.Abs(firstPoint.Start - secondPoint.End))
                    }));

            var match = combinations.OrderBy(item => item.Distance).First();

            if (match.First.Start < match.Second.Start)
            {
                return GetLineSegments(line, match.First.Start, match.First.End, match.Second.Start, match.Second.End);
            }
            else
            {
                return GetLineSegments(line, match.Second.Start, match.Second.End, match.First.Start, match.First.End);
            }
        }

        [ContractVerification(false)]
        private static IList<string> GetLineSegments(string line, int x0, int x1, int x2, int x3)
        {
            return new[]
            {
                line.Substring(0, x0).TrimStart(),
                line.Substring(x0, x1 - x0),
                line.Substring(x1, x2 - x1),
                line.Substring(x2, x3 - x2),
                line.Substring(x3).TrimEnd()
            };
        }

        private static IEnumerable<int> IndexesOfWords(string line, string word)
        {
            var startIndex = 0;

            while (true)
            {
                startIndex = line.IndexOf(word, startIndex, StringComparison.Ordinal);

                if (startIndex < 0)
                    yield break;

                var endIndex = startIndex + word.Length;

                if ((startIndex <= 0) || (IsNonWordChar(line[startIndex - 1])))
                {
                    if ((endIndex >= line.Length) || (IsNonWordChar(line[endIndex])))
                    {
                        yield return startIndex;
                    }
                }

                startIndex = endIndex;
            }
        }

        private static bool IsNonWordChar(char c)
        {
            return !(char.IsLetter(c) || char.IsDigit(c));
        }

        private static string[] ReadAllLines(ProjectFile file)
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

        class SourceFile
        {
            public ProjectFile File { get; set; }

            public string[] Lines { get; set; }
        }

    }
}
