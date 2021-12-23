namespace ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using ResXManager.Model;

    using Throttle;

    using TomsToolbox.Wpf;

    [Export]
    [Shared]
    public sealed class FileWatcher : IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private ImmutableHashSet<string> _changedResourceFiles = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher _watcher = new()
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
        };

        public FileWatcher(ResourceManager resourceManager)
        {
            OnFileChanged();

            _resourceManager = resourceManager;
            _watcher.Changed += File_Changed;
        }

        public void Watch(string? path)
        {
            _watcher.EnableRaisingEvents = false;

            if (string.IsNullOrEmpty(path))
                return;

            _watcher.Path = path;
            _watcher.EnableRaisingEvents = true;
        }

        private void File_Changed(object sender, FileSystemEventArgs e)
        {
            if (!ProjectFileExtensions.IsResourceFile(e.Name))
                return;

            ImmutableInterlocked.Update(ref _changedResourceFiles, (collection, item) => collection.Add(item), e.FullPath);
            OnFileChanged();
        }

        [Throttled(typeof(Throttle), 500)]
        private void OnFileChanged()
        {
            var changedFiles = default(ICollection<string>);

            ImmutableInterlocked.Update(ref _changedResourceFiles, collection =>
            {
                changedFiles = collection;
                return collection.Clear();
            });

            if (changedFiles == null || changedFiles.Count == 0)
            {
                return;
            }

            foreach (var file in changedFiles)
            {
                var language = _resourceManager.ResourceEntities
                    .SelectMany(entity => entity.Languages)
                    .FirstOrDefault(language => string.Equals(language.ProjectFile.FilePath, file, StringComparison.OrdinalIgnoreCase));

                if (language?.ProjectFile.IsBufferOutdated != true)
                    continue;

                if (language.HasChanges)
                    continue;

                var projectFile = language.ProjectFile;
                var entity = language.Container;

                entity.Update(projectFile);
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}
