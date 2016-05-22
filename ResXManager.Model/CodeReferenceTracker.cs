namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;

    using ThreadState = System.Threading.ThreadState;

    [Export]
    public class CodeReferenceTracker
    {
        private Engine _engine;

        private CodeReferenceTracker()
        {
        }

        public int Progress => _engine?.Progress ?? 0;

        public bool IsActive => _engine != null;

        public void StopFind()
        {
            Interlocked.Exchange(ref _engine, null)?.Dispose();
        }

        public void BeginFind(ResourceManager resourceManager, CodeReferenceConfiguration configuration, IEnumerable<ProjectFile> allSourceFiles, ITracer tracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(allSourceFiles != null);

            var sourceFiles = allSourceFiles.Where(item => !item.IsResourceFile() && !item.IsDesignerFile()).ToArray();

            var resourceTableEntries = resourceManager.ResourceEntities
                .Where(entity => !entity.IsWinFormsDesignerResource)
                .SelectMany(entity => entity.Entries).ToArray();

            Interlocked.Exchange(ref _engine, new Engine(configuration, sourceFiles, resourceTableEntries, tracer))?.Dispose();
        }

        private sealed class Engine : IDisposable
        {
            private readonly Thread _backgroundThread;
            private long _total;
            private long _visited;

            public int Progress => (int)(_total > 0 ? Math.Max(1, (100 * _visited) / _total) : 0);

            public Engine(CodeReferenceConfiguration configuration, ICollection<ProjectFile> sourceFiles, ICollection<ResourceTableEntry> resourceTableEntries, ITracer tracer)
            {
                _backgroundThread = new Thread(() => FindCodeReferences(configuration, sourceFiles, resourceTableEntries, tracer))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };

                _backgroundThread.Start();
            }

            private void FindCodeReferences(CodeReferenceConfiguration configuration, ICollection<ProjectFile> projectFiles, ICollection<ResourceTableEntry> resourceTableEntries, ITracer tracer)
            {
                Contract.Requires(configuration != null);
                Contract.Requires(projectFiles != null);
                Contract.Requires(resourceTableEntries != null);

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    _visited = 0;
                    _total = resourceTableEntries.Count + projectFiles.Count();

                    resourceTableEntries.ForEach(entry => entry.CodeReferences = null);

                    var keys = new HashSet<string>(resourceTableEntries.Select(entry => entry.Key));

                    var sourceFiles = projectFiles.AsParallel()
                        .Select(file => new FileInfo(file, configuration.Items, keys, ref _visited))
                        .Where(file => file.HasConfigurations)
                        .ToArray();

                    Contract.Assume(_visited == projectFiles.Count);

                    Contract.Assume(sourceFiles != null); // no contracts in Parallel yet...

                    var keyFilesLookup = sourceFiles.Aggregate(new Dictionary<string, HashSet<FileInfo>>(),
                        (accumulator, file) =>
                        {
                            file.Keys.ForEach(key => accumulator.ForceValue(key, _ => new HashSet<FileInfo>()).Add(file));
                            return accumulator;
                        });

                    Contract.Assume(keyFilesLookup != null);

                    resourceTableEntries.AsParallel().ForAll(entry =>
                    {
                        var key = entry.Key;

                        var files = keyFilesLookup.GetValueOrDefault(key);

                        var references = new List<CodeReference>();

                        files?.ForEach(file => file.FindCodeReferences(entry, references, tracer));

                        entry.CodeReferences = references.ToArray();

                        Interlocked.Increment(ref _visited);
                    });


                    Contract.Assume(_visited == _total);

                    Debug.WriteLine(stopwatch.Elapsed);
                }
                catch (ThreadAbortException)
                {
                }
                finally
                {
                    stopwatch.Stop();
                }
            }

            public void Dispose()
            {
                if (_backgroundThread.ThreadState == ThreadState.Background)
                {
                    _backgroundThread.Abort();
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_backgroundThread != null);
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
            private static readonly Regex _regex = new Regex(@"\W+", RegexOptions.Compiled);
            private readonly ProjectFile _projectFile;
            private readonly string[] _lines;
            private readonly Dictionary<string, HashSet<int>> _keyLinesLookup = new Dictionary<string, HashSet<int>>();
            private readonly CodeReferenceConfigurationItem[] _configurations;

            public FileInfo(ProjectFile projectFile, IEnumerable<CodeReferenceConfigurationItem> configurations, ICollection<string> keys, ref long visited)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(configurations != null);

                _projectFile = projectFile;

                _configurations = configurations
                    .Where(item => item.ParseExtensions().Contains(projectFile.Extension, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                if (_configurations.Any())
                {
                    _lines = _projectFile.ReadAllLines();

                    _lines.ForEach((line, index) =>
                        _regex.Split(line)
                            .Where(keys.Contains)
                            .ForEach(key => _keyLinesLookup.ForceValue(key, _ => new HashSet<int>()).Add(index)));
                }

                Interlocked.Increment(ref visited);
            }

            public bool HasConfigurations => _configurations.Any();

            public IEnumerable<string> Keys => _keyLinesLookup.Keys;

            public void FindCodeReferences(ResourceTableEntry entry, ICollection<CodeReference> references, ITracer tracer)
            {
                Contract.Requires(entry != null);
                Contract.Requires(references != null);
                Contract.Requires(tracer != null);

                var baseName = entry.Container.BaseName;

                try
                {
                    if (_lines == null)
                        return;

                    var key = entry.Key;

                    var lineIndices = _keyLinesLookup.GetValueOrDefault(key);
                    if (lineIndices == null)
                        return;

                    var parameters = _configurations.Select(cfg => new
                    {
                        StringComparison = cfg.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase,
                        Regex = !string.IsNullOrEmpty(cfg.Expression) ? new Regex(cfg.Expression.Replace("$Key", key).Replace("$File", baseName)) : null,
                        cfg.SingleLineComment
                    }).ToArray();


                    lineIndices.ForEach(index =>
                    {
                        var line = _lines[index];
                        var lineNumber = index + 1;

                        foreach (var parameter in parameters)
                        {
                            Contract.Assume(parameter != null);

                            try
                            {
                                var match = new CodeMatch(line, key, parameter.Regex, parameter.StringComparison, parameter.SingleLineComment);
                                if (!match.Success)
                                    continue;

                                references.Add(new CodeReference(_projectFile, lineNumber, match.Segments));
                                break;
                            }
                            catch (Exception ex) // Should not happen, but was reported by someone.
                            {
                                tracer.TraceError("Error detecting code reference in file {0}, line {1} for {2}.{3}\n{4}", _projectFile.FilePath, lineNumber, baseName, key, ex);
                            }
                        }
                    });

                }
                catch (Exception ex) // Should not happen, but was reported by someone.
                {
                    tracer.TraceError("Error detecting code reference in file {0} for {1}\n{2}", _projectFile.FilePath, baseName, ex);
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectFile != null);
                Contract.Invariant(_configurations != null);
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

    internal static class CodeReferenceExtensionMethods
    {
        public static string[] ReadAllLines(this ProjectFile file)
        {
            Contract.Requires(file != null);

            try
            {
                return File.ReadAllLines(file.FilePath);
            }
            catch
            {
            }

            return new string[0];
        }
    }
}