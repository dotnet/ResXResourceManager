namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    using ThreadState = System.Threading.ThreadState;

    [Export, Shared]
    public class CodeReferenceTracker
    {
        private Engine? _engine;

        [ImportingConstructor]
        public CodeReferenceTracker()
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

            public int Progress => (int)(_total <= 0 ? 0 : Math.Max(1, (100 * _visited) / _total));

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
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    _total = resourceTableEntries.Count + projectFiles.Count;

                    resourceTableEntries.ForEach(entry => entry.CodeReferences = null);

                    var keys = new HashSet<string>(resourceTableEntries.Select(entry => entry.Key));

                    var sourceFiles = projectFiles.AsParallel()
                        .Select(file => new FileInfo(file, configuration.Items, keys, ref _visited))
                        .Where(file => file.HasConfigurations)
                        .ToArray();

                    var keyFilesLookup = sourceFiles.Aggregate(new Dictionary<string, HashSet<FileInfo>>(),
                        (accumulator, file) =>
                        {
                            // ReSharper disable PossibleNullReferenceException
                            file.Keys.ForEach(key => accumulator.ForceValue(key, _ => new HashSet<FileInfo>()).Add(file));
                            // ReSharper restore PossibleNullReferenceException
                            return accumulator;
                        });

                    resourceTableEntries.AsParallel().ForAll(entry =>
                    {
                        var key = entry.Key;

                        var files = keyFilesLookup.GetValueOrDefault(key);

                        var references = new List<CodeReference>();

                        files?.ForEach(file => file.FindCodeReferences(entry, references, tracer));

                        entry.CodeReferences = references.AsReadOnly();

                        Interlocked.Increment(ref _visited);
                    });

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
        }

        private class CodeMatch
        {
            public CodeMatch(string line, string key, Regex? regex, StringComparison stringComparison, string? singleLineComment)
            {
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
                        foreach (var group in match.Groups.Cast<Group>().Skip(1).Where(group => group?.Success == true))
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

                if (!singleLineComment.IsNullOrEmpty())
                {
                    var indexOfComment = line.IndexOf(singleLineComment, stringComparison);
                    if ((indexOfComment >= 0) && (indexOfComment <= keyIndexes.FirstOrDefault()))
                        return;
                }

                Success = true;
            }

            public bool Success { get; }

            public IList<string> Segments { get; } = Array.Empty<string>();
        }

        private class FileInfo
        {
            private static readonly Regex _regex = new Regex(@"\W+", RegexOptions.Compiled);
            private readonly ProjectFile _projectFile;
            private readonly Dictionary<string, HashSet<int>> _keyLinesLookup = new Dictionary<string, HashSet<int>>();
            private readonly CodeReferenceConfigurationItem[] _configurations;
            private readonly string[]? _lines;

            public FileInfo(ProjectFile projectFile, IEnumerable<CodeReferenceConfigurationItem> configurations, ICollection<string> keys, ref long visited)
            {
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
                            // ReSharper disable once PossibleNullReferenceException
                            .ForEach(key => _keyLinesLookup.ForceValue(key, _ => new HashSet<int>()).Add(index)));
                }

                Interlocked.Increment(ref visited);
            }

            public bool HasConfigurations => _configurations.Any();

            public IEnumerable<string> Keys => _keyLinesLookup.Keys;

            public void FindCodeReferences(ResourceTableEntry entry, ICollection<CodeReference> references, ITracer tracer)
            {
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
                        Regex = cfg.Expression != null && !string.IsNullOrEmpty(cfg.Expression) ? new Regex(cfg.Expression.Replace("$Key", key, StringComparison.Ordinal).Replace("$File", baseName, StringComparison.Ordinal)) : null,
                        cfg.SingleLineComment
                    }).ToArray();


                    foreach (var index in lineIndices)
                    {
                        var line = _lines[index];
                        var lineNumber = index + 1;

                        foreach (var parameter in parameters)
                        {
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
                    }

                }
                catch (Exception ex) // Should not happen, but was reported by someone.
                {
                    tracer.TraceError("Error detecting code reference in file {0} for {1}\n{2}", _projectFile.FilePath, baseName, ex);
                }
            }
        }
    }

    public class CodeReference
    {
        internal CodeReference(ProjectFile projectFile, int lineNumber, IList<string> lineSegments)
        {
            ProjectFile = projectFile;
            LineNumber = lineNumber;
            LineSegments = lineSegments;
        }

        public int LineNumber { get; }

        public ProjectFile? ProjectFile { get; }

        public IList<string>? LineSegments { get; }
    }

    internal static class CodeReferenceExtensionMethods
    {
        public static string[] ReadAllLines(this ProjectFile file)
        {
            try
            {
                return File.ReadAllLines(file.FilePath);
            }
            catch
            {
                // Ignore any file errors here
            }

            return Array.Empty<string>();
        }
    }
}