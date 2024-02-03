namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.IO;

using TomsToolbox.Essentials;

public abstract class FileWatcher : IDisposable
{
    private readonly object _semaphore = new ();
    private HashSet<string> _changedFiles = new(StringComparer.OrdinalIgnoreCase);

    private readonly FileSystemWatcher _watcher = new()
    {
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastAccess
    };

    protected FileWatcher()
    {
        _watcher.Changed += File_Changed;
        _watcher.Renamed += File_Changed;
        _watcher.Created += File_Changed;
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

    protected ICollection<string> FetchChangedFiles()
    {
        ICollection<string> changedFiles;

        lock (_semaphore)
        {
            changedFiles = _changedFiles;
            _changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return changedFiles;
    }

    private void File_Changed(object? sender, FileSystemEventArgs e)
    {
        var fileName = e.Name;
        if (fileName is null || !IncludeFile(fileName))
            return;

        lock (_semaphore)
        {
            _changedFiles.Add(e.FullPath);
        }
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