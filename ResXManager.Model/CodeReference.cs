namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using DocumentFormat.OpenXml.Drawing;

    public class CodeReference
    {
        private static Thread _backgroundThread;

        private CodeReference(ProjectFile projectFile, int lineNumber, IList<string> lineSegemnts)
        {
            Contract.Requires(projectFile != null);
            Contract.Requires(lineSegemnts != null);

            ProjectFile = projectFile;
            LineNumber = lineNumber;
            LineSegments = lineSegemnts;
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

        public static void BeginFind(ResourceManager resourceManager, IEnumerable<ProjectFile> allSourceFiles, ITracer tracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(allSourceFiles != null);

            StopFind();

            var sourceFiles = allSourceFiles.Where(item => !item.IsResourceFile() && !item.IsDesignerFile()).ToArray();
            var resourceTableEntries = resourceManager.ResourceEntities.SelectMany(entity => entity.Entries).ToArray();
            var configuration = resourceManager.Configuration.CodeReferences;

            _backgroundThread = new Thread(() => FindCodeReferences(configuration, sourceFiles, resourceTableEntries, tracer))
            {
                IsBackground = true, 
                Priority = ThreadPriority.Lowest
            };

            _backgroundThread.Start();
        }

        public static void FindCodeReferences(CodeReferenceConfiguration configuration, IEnumerable<ProjectFile> projectFiles, IList<ResourceTableEntry> resourceTableEntries, ITracer tracer)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(projectFiles != null);
            Contract.Requires(resourceTableEntries != null);

            try
            {
                foreach (var entry in resourceTableEntries)
                {
                    Contract.Assume(entry != null);
                    entry.CodeReferences = null;
                }

                var sourceFiles = projectFiles.Select(file => new FileInfo(file)).ToArray();
                var entriesByBaseName = resourceTableEntries.GroupBy(entry => entry.Owner.BaseName);

                foreach (var entriesGroup in entriesByBaseName.AsParallel())
                {
                    Contract.Assume(entriesGroup != null);
                    var baseName = entriesGroup.Key;
                    Contract.Assume(baseName != null);
                    var tableEntries = entriesGroup.ToArray();

                    foreach (var sourceFile in sourceFiles)
                    {
                        Contract.Assume(sourceFile != null);

                        var configs = configuration.Items
                            .Where(item => item.ParseExtensions().Contains(sourceFile.ProjectFile.Extension, StringComparer.OrdinalIgnoreCase))
                            .ToArray();

                        if (!configs.Any())
                            continue;
                        
                        FindCodeReferences(configs, sourceFile, baseName, tableEntries, tracer);
                    }
                }

                foreach (var entry in resourceTableEntries.Where(entry => entry.CodeReferences == null))
                {
                    Contract.Assume(entry != null);
                    // Show 0 code references in UI
                    entry.CodeReferences = new CodeReference[0];
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private static void FindCodeReferences(IList<CodeReferenceConfigurationItem> configurations, FileInfo source, string baseName, IList<ResourceTableEntry> entries, ITracer tracer)
        {
            Contract.Requires(configurations != null);
            Contract.Requires(source != null);
            Contract.Requires(baseName != null);
            Contract.Requires(entries != null);

            try
            {
                var projectFile = source.ProjectFile;

                foreach (var entry in entries)
                {
                    Contract.Assume(entry != null);
                    var key = entry.Key;

                    var parameters = configurations.Select(cfg => new
                    {
                        StringComparison = cfg.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase,
                        Regex = !string.IsNullOrEmpty(cfg.Expression) ? new Regex(cfg.Expression.Replace("$Key", key).Replace("$File", baseName)) : null,
                        cfg.SingleLineComment
                    }).ToArray();

                    var lineNumber = 0;

                    foreach (var line in source.Lines)
                    {
                        Contract.Assume(line != null);
                        lineNumber += 1;

                        foreach (var parameter in parameters)
                        {
                            Contract.Assume(parameter != null);
                            try
                            {
                                var match = new CodeMatch(line, key, parameter.Regex, parameter.StringComparison, parameter.SingleLineComment);
                                if (!match.Success)
                                    continue;

                                var codeReference = new CodeReference(projectFile, lineNumber, match.Segments);

                                var codeReferences = entry.CodeReferences ?? (entry.CodeReferences = new ObservableCollection<CodeReference>());
                                codeReferences.Add(codeReference);
                                break;
                            }
                            catch (Exception ex) // Should not happen, but was reported by someone.
                            {
                                tracer.TraceError("Error detecting code reference in file {0}, line {1} for {2}.{3}\n{4}", projectFile.FilePath, lineNumber, baseName, key, ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) // Should not happen, but was reported by someone.
            {
                tracer.TraceError("Error detecting code reference in file {0} for {1}\n{2}", source.ProjectFile.FilePath, baseName, ex);
            }
        }

        class CodeMatch
        {
            [ContractVerification(false)] // too many assumptions would be needed...
            public CodeMatch(string line, string key, Regex regex, StringComparison stringComparison, string singleLineComment)
            {
                Contract.Requires(line != null);
                Contract.Requires(key != null);

                var indexOfKey = line.IndexOf(key, stringComparison);
                if (indexOfKey < 0)
                    return;

                var lastIndex = 0;

                if (regex == null)
                {
                    lastIndex = indexOfKey;
                    var length = key.Length;

                    Segments = new[] { line.Substring(0, lastIndex), line.Substring(lastIndex, length), line.Substring(lastIndex + length) };
                }
                else
                {
                    line = " " + line + " ";

                    var match = regex.Match(line);
                    if (!match.Success)
                        return;

                    if (match.Groups.Count < 2)
                    {
                        lastIndex = match.Index;
                        var length = match.Length;

                        Segments = new[] { line.Substring(0, lastIndex), line.Substring(lastIndex, length), line.Substring(lastIndex + length) };
                    }
                    else
                    {
                        Segments = new List<string>();
                        var index = 0;
                        foreach (var group in match.Groups.Cast<Group>().Skip(1).Where(group => group.Success))
                        {
                            Segments.Add(line.Substring(index, group.Index - index));
                            index = group.Index;
                            lastIndex = index;
                            Segments.Add(line.Substring(index, group.Length));
                            index += group.Length;
                        }

                        Segments.Add(line.Substring(index));
                    }
                }

                if (!string.IsNullOrEmpty(singleLineComment))
                {
                    var indexOfComment = line.IndexOf(singleLineComment, stringComparison);
                    if ((indexOfComment >= 0) && (indexOfComment <= lastIndex))
                        return;
                }

                Success = true;
            }

            public bool Success
            {
                get;
                private set;
            }

            public IList<string> Segments
            {
                get;
                private set;
            }

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(!Success || (Segments != null));
            }
        }

        class FileInfo
        {
            public FileInfo(ProjectFile projectFile)
            {
                Contract.Requires(projectFile != null);

                ProjectFile = projectFile;
                Lines = projectFile.ReadAllLines();
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

        public static IList<string> GetSegments(this string line, int x0, int x1, int x2, int x3)
        {
            Contract.Requires(line != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            Contract.Ensures(Contract.Result<IList<string>>().Count == 5);
            Contract.Ensures(Contract.Result<IList<string>>().All(x => x != null));

            Contract.Assume(0 <= x0);
            Contract.Assume(x0 <= x1);
            Contract.Assume(x1 <= x2);
            Contract.Assume(x2 <= x3);
            Contract.Assume(x3 <= line.Length);

            IList<string> segments = new[]
            {
                line.Substring(0, x0).TrimStart(),
                line.Substring(x0, x1 - x0),
                line.Substring(x1, x2 - x1),
                line.Substring(x2, x3 - x2),
                line.Substring(x3).TrimEnd()
            };

            Contract.Assume(segments.Count == 5);

            return segments;
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

                if ((startIndex == 0) || IsNonWordChar(line[startIndex - 1]))
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
    }
}
