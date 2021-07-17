namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    [Shared]
    [Export(typeof(IErrorListProvider))]
    internal sealed class ErrorListProviderService : IErrorListProvider
    {
        private bool _isDisabled;
        private readonly ErrorListProvider _errorListProvider;
        private readonly TaskProvider.TaskCollection _tasks;

        public event Action<ResourceTableEntry>? Navigate;

        public ErrorListProviderService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _errorListProvider = new ErrorListProvider(ServiceProvider.GlobalProvider)
            {
                ProviderName = "ResX Resource Manager",
                ProviderGuid = new Guid("36C23699-DA00-4D2C-8233-5484A091BFD2")
            };

            _tasks = _errorListProvider.Tasks;
        }

        public void SetEntries(ICollection<ResourceTableEntry> entries, ICollection<CultureKey> cultures, int errorCategory)
        {
            if (_isDisabled)
                return;

            try
            {
                SetInternal(entries, cultures, errorCategory);
            }
            catch (SystemException)
            {
                _isDisabled = true;
            }
        }

        private void SetInternal(ICollection<ResourceTableEntry> entries, ICollection<CultureKey> cultures, int errorCategory)
        {
            _errorListProvider.SuspendRefresh();

            try
            {
                _tasks.Clear();

                var errorCount = 0;

                foreach (var entry in entries)
                {
                    foreach (var culture in cultures)
                    {
                        if (!entry.GetError(culture, out var error))
                            continue;

                        if (++errorCount >= 200)
                            return;

                        // Bug in VS2022: : this is the call that is responsible for the exception: 'Could not load type 'Microsoft.VisualStudio.Shell.Task' from assembly 'Microsoft.VisualStudio.Shell.15.0, Version=17.0.0.0
                        var task = new ResourceErrorTask(entry)
                        {
                            ErrorCategory = (TaskErrorCategory) errorCategory,
                            Category = TaskCategory.BuildCompile,
                            Text = error,
                            Document = entry.Container.UniqueName,
                        };

                        task.Navigate += Task_Navigate;
                        _tasks.Add(task);
                    }
                }
            }
            finally
            {
                _errorListProvider.ResumeRefresh();
            }
        }

        public void Remove(ResourceTableEntry entry)
        {
            if (_isDisabled)
                return;

            try
            {
                RemoveInternal(entry);
            }
            catch (SystemException)
            {
                _isDisabled = true;
            }
        }

        private void RemoveInternal(ResourceTableEntry entry)
        {
            var task = _tasks.OfType<ResourceErrorTask>().FirstOrDefault(t => t.Entry == entry);

            if (task == null)
                return;

            _tasks.Remove(task);
        }


        public void Clear()
        {
            if (_isDisabled)
                return;

            try
            {
                ClearInternal();
            }
            catch (SystemException)
            {
                _isDisabled = true;
            }
        }

        private void ClearInternal()
        {
            _tasks.Clear();
        }

        private void Task_Navigate(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(sender is ResourceErrorTask task))
                return;

            var entry = task.Entry;
            if (entry == null)
                return;

            Navigate?.Invoke(entry);
        }

        private class ResourceErrorTask : ErrorTask
        {
            public ResourceErrorTask(ResourceTableEntry entry)
            {
                Entry = entry;
            }

            public ResourceTableEntry? Entry { get; }
        }

        public void Dispose()
        {
            _errorListProvider.Dispose();
        }
    }
}