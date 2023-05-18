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

                        var task = new ResourceErrorTask(entry)
                        {
                            ErrorCategory = (TaskErrorCategory)errorCategory,
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
            var task = _tasks.OfType<ResourceErrorTask>().FirstOrDefault(t => t.Entry == entry);

            if (task == null)
                return;

            _tasks.Remove(task);
        }


        public void Clear()
        {
            _tasks.Clear();
        }

        private void Task_Navigate(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not ResourceErrorTask task)
                return;

            var entry = task.Entry;
            if (entry == null)
                return;

            Navigate?.Invoke(entry);
        }

        private sealed class ResourceErrorTask : ErrorTask
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