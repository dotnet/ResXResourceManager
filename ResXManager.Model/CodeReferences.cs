namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    public static class CodeReferences
    {
        private static Thread _backgroundThread;

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

        private static void FindCodeReferences(IEnumerable<ProjectFile> sourceFiles, IEnumerable<ResourceTableEntry> resourceTableEntries)
        {
            Contract.Requires(sourceFiles != null);
            Contract.Requires(resourceTableEntries != null);

            try
            {
                /*/
                // Using regular expressions is toooooo slow, just kep for reference
                foreach (var entry in resourceTableEntries)
                {
                    var sourceFilesContent = sourceFiles.Select(ReadAllText);

                    var regex = new Regex(string.Format(CultureInfo.InvariantCulture, @"[^\w]+{0}[\.\^]{1}[^\w]+", entry.Owner.BaseName, entry.Key));

                    entry.CodeReferences = sourceFilesContent.Sum(text => regex.Matches(text).Count);
                }
                /*/
                var sourceFilesContent = string.Join(Environment.NewLine, sourceFiles.Select(ReadAllText));

                var entriesByBaseName = resourceTableEntries.GroupBy(entry => entry.Owner.BaseName);

                foreach (var baseNameGroup in entriesByBaseName.AsParallel())
                {
                    var index = 0;

                    // reference must have any statement on the left side, so it's ok to start with '> 0'
                    var baseName = baseNameGroup.Key;

                    while ((index = sourceFilesContent.IndexOf(baseName, index, StringComparison.Ordinal)) > 0)
                    {
                        var startIndex = index;
                        index += baseName.Length;

                        if (index + 2 >= sourceFilesContent.Length)
                            break;

                        if (!IsNonWordChar(sourceFilesContent[startIndex - 1]))
                            continue;

                        var c = sourceFilesContent[index];
                        if (c == '-')
                        {
                            index++;
                            c = sourceFilesContent[index];
                            if (c != '>') // c++: BaseName->Key
                                continue;
                        }
                        else if (c != '.') // c#, vb: BaseName.Key
                        {
                            continue;
                        }

                        index++;

                        foreach (var entry in baseNameGroup)
                        {
                            if (IsWordMatch(sourceFilesContent, index, entry.Key))
                            {
                                entry.CodeReferences = entry.CodeReferences.GetValueOrDefault() + 1;
                            }
                        }
                    }

                    foreach (var entry in baseNameGroup.Where(entry => entry.CodeReferences == null))
                    {
                        entry.CodeReferences = 0;
                    }
                }
                //*/
            }
            catch (ThreadAbortException)
            {
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool IsWordMatch(string text, int index, string key)
        {
            Contract.Requires(text != null);
            Contract.Requires(key != null);
            Contract.Requires(index > 0);

            if (text.Length <= index + key.Length)
                return false;

            if (!IsNonWordChar(text[index + key.Length]))
                return false;

            return !key.Where((t, offest) => text[index + offest] != t).Any();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool IsNonWordChar(char c)
        {
            return !(char.IsLetter(c) || char.IsDigit(c));
        }

        private static string ReadAllText(ProjectFile file)
        {
            Contract.Requires(file != null);
            try
            {
                Thread.Sleep(1);
                return File.ReadAllText(file.FilePath);
            }
            catch
            {
            }

            return string.Empty;
        }


    }
}
