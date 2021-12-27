namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using TomsToolbox.Essentials;

    public abstract class FileWatcher : IDisposable
    {
        private ImmutableHashSet<string> _changedFiles = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher _watcher = new()
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
        };

        protected FileWatcher()
        {
            _watcher.Changed += File_Changed;
        }

        public string? Folder { get; private set; }

        protected void Watch(string? folder)
        {
            Folder = folder;

            _watcher.EnableRaisingEvents = false;

            if (string.IsNullOrEmpty(folder))
                return;

            _watcher.Path = folder;
            _watcher.EnableRaisingEvents = true;
        }

        protected abstract void OnFilesChanged();

        protected abstract bool IncludeFile(string fileName);

        protected ICollection<string> GetChangedFiles()
        {
            var changedFiles = default(ICollection<string>);

            ImmutableInterlocked.Update(ref _changedFiles, collection =>
            {
                changedFiles = collection;
                return collection.Clear();
            });

            return changedFiles ?? Array.Empty<string>();
        }

        private void File_Changed(object sender, FileSystemEventArgs e)
        {
            if (!IncludeFile(e.Name))
                return;

            ImmutableInterlocked.Update(ref _changedFiles, (collection, item) => collection.Add(item), e.FullPath);

            OnFilesChanged();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watcher.Dispose();
            }
            else
            {
                this.ReportNotDisposedObject();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FileWatcher()
        {
            Dispose(false);
        }
    }
}