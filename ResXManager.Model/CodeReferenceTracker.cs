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

    using JetBrains.Annotations;

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

        public void BeginFind([NotNull] ResourceManager resourceManager, [NotNull] CodeReferenceConfiguration configuration, [ItemNotNull][NotNull] IEnumerable<ProjectFile> allSourceFiles, [NotNull] ITracer tracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(allSourceFiles != null);
            Contract.Requires(tracer != null);

            var sourceFiles = allSourceFiles.Where(item => !item.IsResourceFile() && !item.IsDesignerFile()).ToArray();

            var resourceTableEntries = resourceManager.ResourceEntities
                .Where(entity => !entity.IsWinFormsDesignerResource)
                .SelectMany(entity => entity.Entries).ToArray();

            Interlocked.Exchange(ref _engine, new Engine(configuration, sourceFiles, resourceTableEntries, tracer))?.Dispose();
        }

        private sealed class Engine : IDisposable
        {
            [NotNull]
            private readonly Thread _backgroundThread;
            private long _total;
            private long _visited;

            public int Progress => (int)(_total <= 0 ? 0 : Math.Max(1, (100 * _visited) / _total));

            public Engine([NotNull] CodeReferenceConfiguration configuration, [NotNull] ICollection<ProjectFile> sourceFiles, [NotNull] ICollection<ResourceTableEntry> resourceTableEntries, [NotNull] ITracer tracer)
            {
                Contract.Requires(configuration != null);
                Contract.Requires(sourceFiles != null);
                Contract.Requires(resourceTableEntries != null);
                Contract.Requires(tracer != null);

                _backgroundThread = new Thread(() => FindCodeReferences(configuration, sourceFiles, resourceTableEntries, tracer))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };

                _backgroundThread.Start();
            }

            private void FindCodeReferences([NotNull] CodeReferenceConfiguration configuration, [NotNull, ItemNotNull] ICollection<ProjectFile> projectFiles, [NotNull, ItemNotNull] ICollection<ResourceTableEntry> resourceTableEntries, [NotNull] ITracer tracer)
            {
                Contract.Requires(configuration != null);
                Contract.Requires(projectFiles != null);
                Contract.Requires(resourceTableEntries != null);
                Contract.Requires(tracer != null);

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    _total = resourceTableEntries.Count + projectFiles.Count;

                    // ReSharper disable once PossibleNullReferenceException
                    resourceTableEntries.ForEach(entry => entry.CodeReferences = null);

                    var keys = new HashSet<string>(resourceTableEntries.Select(entry => entry.Key));

                    var sourceFiles = projectFiles.AsParallel()
                        // ReSharper disable once AssignNullToNotNullAttribute
                        .Select(file => new FileInfo(file, configuration.Items, keys, ref _visited))
                        // ReSharper disable once PossibleNullReferenceException
                        .Where(file => file.HasConfigurations)
                        .ToArray();

                    Contract.Assume(_visited == projectFiles.Count);

                    Contract.Assume(sourceFiles != null); // no contracts in Parallel yet...

                    var keyFilesLookup = sourceFiles.Aggregate(new Dictionary<string, HashSet<FileInfo>>(),
                        (accumulator, file) =>
                        {
                            // ReSharper disable PossibleNullReferenceException
                            file.Keys.ForEach(key => accumulator.ForceValue(key, _ => new HashSet<FileInfo>()).Add(file));
                            // ReSharper restore PossibleNullReferenceException
                            return accumulator;
                        });

                    Contract.Assume(keyFilesLookup != null);

                    resourceTableEntries.AsParallel().ForAll(entry =>
                    {
                        Contract.Assume(entry != null);
                        var key = entry.Key;

                        var files = keyFilesLookup.GetValueOrDefault(key);

                        var references = new List<CodeReference>();

                        // ReSharper disable once PossibleNullReferenceException
                        files?.ForEach(file => file.FindCodeReferences(entry, references, tracer));

                        entry.CodeReferences = references.AsReadOnly();

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
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_backgroundThread != null);
            }
        }

        private class CodeMatch
        {
            private readonly IList<string> _segments;

            [ContractVerification(false)] // too many assumptions would be needed...
            public CodeMatch([NotNull] string line, [NotNull] string key, Regex regex, StringComparison stringComparison, string singleLineComment)
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

                    _segments = new[] { line.Substring(0, keyIndex), line.Substring(keyIndex, length), line.Substring(keyIndex + length) };
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

                        _segments = new[] { line.Substring(0, keyIndex), line.Substring(keyIndex, length), line.Substring(keyIndex + length) };
                    }
                    else
                    {
                        _segments = new List<string>();
                        var keyIndex = 0;
                        foreach (var group in match.Groups.Cast<Group>().Skip(1).Where(group => group?.Success == true))
                        {
                            _segments.Add(line.Substring(keyIndex, group.Index - keyIndex));
                            keyIndex = group.Index;
                            keyIndexes.Add(keyIndex);
                            _segments.Add(line.Substring(keyIndex, group.Length));
                            keyIndex += group.Length;
                        }

                        _segments.Add(line.Substring(keyIndex));
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

            public bool Success { get; }

            public IList<string> Segments
            {
                get
                {
                    Contract.Ensures(!Success || (Contract.Result<IList<string>>() != null));

                    return _segments;
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(!Success || (_segments != null));
            }
        }

        private class FileInfo
        {
            [NotNull]
            private static readonly Regex _regex = new Regex(@"\W+", RegexOptions.Compiled);
            [NotNull]
            private readonly ProjectFile _projectFile;
            [NotNull]
            private readonly Dictionary<string, HashSet<int>> _keyLinesLookup = new Dictionary<string, HashSet<int>>();
            [NotNull, ItemNotNull]
            private readonly CodeReferenceConfigurationItem[] _configurations;

            private readonly string[] _lines;

            public FileInfo([NotNull] ProjectFile projectFile, [NotNull, ItemNotNull] IEnumerable<CodeReferenceConfigurationItem> configurations, [NotNull] ICollection<string> keys, ref long visited)
            {
                Contract.Requires(projectFile != null);
                Contract.Requires(configurations != null);
                Contract.Requires(keys != null);

                _projectFile = projectFile;

                _configurations = configurations
                    .Where(item => item.ParseExtensions().Contains(projectFile.Extension, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                if (_configurations.Any())
                {
                    _lines = _projectFile.ReadAllLines();

                    _lines.ForEach((line, index) =>
                        // ReSharper disable once AssignNullToNotNullAttribute
                        _regex.Split(line)
                            .Where(keys.Contains)
                            // ReSharper disable once PossibleNullReferenceException
                            .ForEach(key => _keyLinesLookup.ForceValue(key, _ => new HashSet<int>()).Add(index)));
                }

                Interlocked.Increment(ref visited);
            }

            public bool HasConfigurations => _configurations.Any();

            [NotNull]
            public IEnumerable<string> Keys => _keyLinesLookup.Keys;

            public void FindCodeReferences([NotNull] ResourceTableEntry entry, [NotNull] ICollection<CodeReference> references, [NotNull] ITracer tracer)
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


                    foreach (var index in lineIndices)
                    {
                        Contract.Assume((index >= 0) && (index < _lines.Length));

                        var line = _lines[index];
                        var lineNumber = index + 1;

                        foreach (var parameter in parameters)
                        {
                            Contract.Assume(parameter != null);

                            try
                            {
                                Contract.Assume(line != null);

                                var match = new CodeMatch(line, key, parameter.Regex, parameter.StringComparison, parameter.SingleLineComment);
                                if (!match.Success)
                                    continue;

                                var segemnts = match.Segments;

                                references.Add(new CodeReference(_projectFile, lineNumber, segemnts));
                                break;
                            }
                            catch (Exception ex) // Should not happen, but was reported by someone.
                            {
                                tracer.TraceError("Error detecting code reference in file {0}, line {1} for {2}.{3}\n{4}", _projectFile.FilePath, lineNumber, baseName, key, ex);
                            }
                        }
                    }

                }
                catch (Exception ex) // Should not happen, but was reported by someone.
                {
                    tracer.TraceError("Error detecting code reference in file {0} for {1}\n{2}", _projectFile.FilePath, baseName, ex);
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_projectFile != null);
                Contract.Invariant(_keyLinesLookup != null);
                Contract.Invariant(_configurations != null);
                Contract.Invariant(_regex != null);
            }
        }
    }

    public class CodeReference
    {
        internal CodeReference([NotNull] ProjectFile projectFile, int lineNumber, [NotNull] IList<string> lineSegemnts)
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
        [NotNull]
        public static string[] ReadAllLines([NotNull] this ProjectFile file)
        {
            Contract.Requires(file != null);
            Contract.Ensures(Contract.Result<string[]>() != null);

            try
            {
                return File.ReadAllLines(file.FilePath);
            }
            catch
            {
                // Ignore any file errors here
            }

            return new string[0];
        }
    }
}