namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using tomenglertde.ResXManager.Infrastructure;

    [Export]
    public class CodeReferenceTracker
    {
        private Thread _backgroundThread;
        private long _total;
        private long _visited;

        private CodeReferenceTracker()
        {
        }

        public int Progress
        {
            get
            {
                return (int)(_total > 0 ? Math.Max(1, (100 * _visited) / _total) : 0);
            }
        }

        public void StopFind()
        {
            if ((_backgroundThread == null) || (_backgroundThread.ThreadState != ThreadState.Background))
                return;

            _backgroundThread.Abort();
            _backgroundThread = null;
        }

        public void BeginFind(ResourceManager resourceManager, CodeReferenceConfiguration configuration, IEnumerable<ProjectFile> allSourceFiles, ITracer tracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(allSourceFiles != null);

            StopFind();

            var sourceFiles = allSourceFiles.Where(item => !item.IsResourceFile() && !item.IsDesignerFile()).ToArray();
            var resourceTableEntries = resourceManager.ResourceEntities.SelectMany(entity => entity.Entries).ToArray();

            _backgroundThread = new Thread(() => FindCodeReferences(configuration, sourceFiles, resourceTableEntries, tracer))
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };

            _backgroundThread.Start();
        }

        private void FindCodeReferences(CodeReferenceConfiguration configuration, IEnumerable<ProjectFile> projectFiles, IList<ResourceTableEntry> resourceTableEntries, ITracer tracer)
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

                var sourceFiles = projectFiles.Select(file => new FileInfo(file, configuration.Items))
                    .Where(file => file.Configurations.Any())
                    .ToArray();

                _total = sourceFiles.Select(sf => sf.Lines.Length).Sum() * resourceTableEntries.Count;
                _visited = 0;

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

                        FindCodeReferences(sourceFile, baseName, tableEntries, tracer);
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

        private void FindCodeReferences(FileInfo source, string baseName, IList<ResourceTableEntry> entries, ITracer tracer)
        {
            Contract.Requires(source != null);
            Contract.Requires(baseName != null);
            Contract.Requires(entries != null);

            try
            {
                var projectFile = source.ProjectFile;
                var configurations = source.Configurations;

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

                        Interlocked.Increment(ref _visited);

                        if (line.IndexOf(key, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

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

        private class CodeMatch
        {
            [ContractVerification(false)] // too many assumptions would be needed...
            public CodeMatch(string line, string key, Regex regex, StringComparison stringComparison, string singleLineComment)
            {
                Contract.Requires(line != null);
                Contract.Requires(key != null);

                var keyIndexes = new List<int>();

                if (regex == null)
                {
                    var keyIndex = line.IndexOf(key, stringComparison);
                    if (keyIndex < 0)
                        return;

                    keyIndexes.Add(keyIndex);

                    var length = key.Length;

                    Segments = new[] { line.Substring(0, keyIndex), line.Substring(keyIndex, length), line.Substring(keyIndex + length) };
                }
                else
                {
                    line = " " + line + " ";

                    var match = regex.Match(line);
                    if (!match.Success)
                        return;

                    if (match.Groups.Count < 2)
                    {
                        var keyIndex = match.Index;
                        var length = match.Length;

                        keyIndexes.Add(keyIndex);

                        Segments = new[] { line.Substring(0, keyIndex), line.Substring(keyIndex, length), line.Substring(keyIndex + length) };
                    }
                    else
                    {
                        Segments = new List<string>();
                        var keyIndex = 0;
                        foreach (var group in match.Groups.Cast<Group>().Skip(1).Where(group => group.Success))
                        {
                            Segments.Add(line.Substring(keyIndex, group.Index - keyIndex));
                            keyIndex = group.Index;
                            keyIndexes.Add(keyIndex);
                            Segments.Add(line.Substring(keyIndex, group.Length));
                            keyIndex += group.Length;
                        }

                        Segments.Add(line.Substring(keyIndex));
                    }
                }

                if (!string.IsNullOrEmpty(singleLineComment))
                {
                    var indexOfComment = line.IndexOf(singleLineComment, stringComparison);
                    if ((indexOfComment >= 0) && (indexOfComment <= keyIndexes.FirstOrDefault()))
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
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(!Success || (Segments != null));
            }
        }

        private class FileInfo
        {
            private string[] _lines;

            public FileInfo(ProjectFile projectFile, IEnumerable<CodeReferenceConfigurationItem> configurations)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(configurations != null);

                ProjectFile = projectFile;
                Configurations = configurations
                    .Where(item => item.ParseExtensions().Contains(projectFile.Extension, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            public ProjectFile ProjectFile
            {
                get;
                private set;
            }

            public string[] Lines
            {
                get
                {
                    Contract.Ensures(Contract.Result<string[]>() != null);
                    return _lines ?? (_lines = ProjectFile.ReadAllLines());
                }
            }

            public CodeReferenceConfigurationItem[] Configurations
            {
                get;
                private set;
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(ProjectFile != null);
                Contract.Invariant(Configurations != null);
            }
        }
    }

    public class CodeReference
    {
        internal CodeReference(ProjectFile projectFile, int lineNumber, IList<string> lineSegemnts)
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
    }

    static class CodeReferenceExtensionMethods
    {
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
