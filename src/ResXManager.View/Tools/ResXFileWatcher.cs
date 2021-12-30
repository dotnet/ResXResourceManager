namespace ResXManager.View.Tools
{
    using System;
    using System.Composition;
    using System.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export(typeof(IService)), Shared]
    internal sealed class ResXFileWatcher : FileWatcher, IService
    {
        private readonly ResourceManager _resourceManager;

        public ResXFileWatcher(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _resourceManager.SolutionFolderChanged += ResourceManager_SolutionFolderChanged;
        }

        public void Start()
        {
            OnFilesChanged();
        }

        private void ResourceManager_SolutionFolderChanged(object? sender, TextEventArgs e)
        {
            Watch(e.Text);
        }

        protected override bool IncludeFile(string fileName)
        {
            return ProjectFileExtensions.IsResourceFile(fileName);
        }

        [Throttled(typeof(Throttle), 500)]
        protected override void OnFilesChanged()
        {
            if (_resourceManager.IsLoading)
            {
                OnFilesChanged();
                return;
            }

            var changedFiles = GetChangedFiles();
            if (changedFiles.Count == 0)
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

                if (entity.Update(projectFile, out var updatedLanguage))
                {
                    _resourceManager.OnProjectFileSaved(updatedLanguage, projectFile);
                }
            }
        }
    }
}
