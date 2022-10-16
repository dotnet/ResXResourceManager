namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using PropertyChanged;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;

    /// <summary>
    /// Represents all resources found in a folder and it's sub folders.
    /// </summary>
    [Shared]
    [Export]
    public sealed class ResourceManager : INotifyPropertyChanged
    {
        private string? _snapshot;

        public event EventHandler<ResourceBeginEditingEventArgs>? BeginEditing;
        public event EventHandler<CancelEventArgs>? Reloading;
        public event EventHandler<EventArgs>? Loaded;
        public event EventHandler<LanguageEventArgs>? LanguageChanged;
        public event EventHandler<LanguageEventArgs>? LanguageAdded;
        public event EventHandler<ProjectFileEventArgs>? ProjectFileSaved;
        public event EventHandler<TextEventArgs>? SolutionFolderChanged;

        [ImportingConstructor]
        public ResourceManager(IConfiguration configuration, ITracer tracer)
        {
            Configuration = configuration;
            Tracer = tracer;
            TableEntries = ResourceEntities.ObservableSelectMany(entity => entity.Entries);
        }

        public IList<ProjectFile> AllSourceFiles { get; private set; } = Array.Empty<ProjectFile>();

        public bool IsLoading { get; private set; }

        private async Task<bool> LoadAsync(IList<ProjectFile> allSourceFiles, CancellationToken? cancellationToken)
        {
            AllSourceFiles = allSourceFiles;

            var resourceFilesByDirectory = allSourceFiles
                .Where(file => file.IsResourceFile())
                .GroupBy(file => file.GetBaseDirectory())
                .ToList();

            return await InternalLoadAsync(resourceFilesByDirectory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves all modified resource files.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            var changedResourceLanguages = ResourceEntities
                .SelectMany(entity => entity.Languages)
                .Where(lang => lang.HasChanges)
                .ToArray();

            changedResourceLanguages.ForEach(resourceLanguage => resourceLanguage.Save());
        }

        /// <summary>
        /// Gets the loaded resource entities.
        /// </summary>
        public ObservableCollection<ResourceEntity> ResourceEntities { get; } = new();

        /// <summary>
        /// Gets the table entries of all entities.
        /// </summary>
        public IObservableCollection<ResourceTableEntry> TableEntries { get; }

        /// <summary>
        /// Gets the cultures of all entities.
        /// </summary>
        public ObservableCollection<CultureKey> Cultures { get; } = new();

        public ITracer Tracer { get; }

        public IConfiguration Configuration { get; }

        public bool HasChanges => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.HasChanges);

        public bool IsSaving => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.IsSaving);

        [OnChangedMethod(nameof(OnSolutionFolderChanged))]
        public string? SolutionFolder { get; private set; }

        private void OnSolutionFolderChanged()
        {
            SolutionFolderChanged?.Invoke(this, new TextEventArgs(SolutionFolder ?? string.Empty));
        }

        public void ReloadSnapshot()
        {
            if (!_snapshot.IsNullOrEmpty())
                ResourceEntities.LoadSnapshot(_snapshot);
        }

        public async Task<bool> ReloadAsync(string? solutionFolder, IList<ProjectFile> sourceFiles, CancellationToken? cancellationToken)
        {
            try
            {
                IsLoading = true;

                var args = new CancelEventArgs();
                Reloading?.Invoke(this, args);
                if (args.Cancel)
                    return false;

                if (!string.Equals(solutionFolder, SolutionFolder, StringComparison.OrdinalIgnoreCase))
                {
                    ResourceEntities.Clear();
                }

                SolutionFolder = solutionFolder;
                return await LoadAsync(sourceFiles, cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                IsLoading = true;

                SolutionFolder = null;
                await LoadAsync(Array.Empty<ProjectFile>(), CancellationToken.None).ConfigureAwait(true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool CanEdit(ResourceEntity resourceEntity, CultureKey? cultureKey)
        {
            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, cultureKey);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        private async Task<bool> InternalLoadAsync(ICollection<IGrouping<string, ProjectFile>> resourceFilesByDirectory, CancellationToken? cancellationToken)
        {
            if (!await LoadEntitiesAsync(resourceFilesByDirectory, cancellationToken).ConfigureAwait(true))
                return false; // nothing has changed, no need to continue

            if (!_snapshot.IsNullOrEmpty())
                ResourceEntities.LoadSnapshot(_snapshot);

            var cultureKeys = ResourceEntities
                .SelectMany(entity => entity.Languages)
                .Select(lang => lang.CultureKey)
                .Distinct()
                .OrderBy(item => item.Culture?.DisplayName)
                .ToArray();

            Cultures.SynchronizeWith(cultureKeys);

            OnLoaded();

            return true;
        }

        private async Task<bool> LoadEntitiesAsync(ICollection<IGrouping<string, ProjectFile>> fileNamesByDirectory, CancellationToken? cancellationToken)
        {
            static string GenerateKey(string projectName, string baseName, string directoryName)
            {
                return string.Join("|", projectName, baseName, directoryName);
            }

            var unmatchedEntities = ResourceEntities.ToList();
            var existingEntities = ResourceEntities.ToDictionary(entity => GenerateKey(entity.ProjectName, entity.BaseName, entity.DirectoryName), StringComparer.OrdinalIgnoreCase);
            var newEntities = new List<ResourceEntity>();
            var entitiesToUpdate = new List<Tuple<ResourceEntity, ProjectFile[]>>();

            var duplicateKeyHandling = Configuration.DuplicateKeyHandling;
            var neutralResourcesLanguage = Configuration.NeutralResourcesLanguage;

            void Load()
            {
                foreach (var directory in fileNamesByDirectory)
                {
                    var directoryName = directory.Key;
                    var filesByBaseName = directory.GroupBy(file => file.GetBaseName(), StringComparer.OrdinalIgnoreCase);

                    foreach (var files in filesByBaseName)
                    {
                        if (!files.Any())
                            continue;

                        var baseName = files.Key;
                        var filesByProject = files.GroupBy(file => file.ProjectName);

                        foreach (var item in filesByProject)
                        {
                            cancellationToken?.ThrowIfCancellationRequested();

                            var projectName = item.Key;
                            var projectFiles = item.ToArray();

                            if (projectName.IsNullOrEmpty() || !projectFiles.Any())
                                continue;

                            if (existingEntities.TryGetValue(GenerateKey(projectName, baseName, directoryName), out var existingEntity))
                            {
                                entitiesToUpdate.Add(new Tuple<ResourceEntity, ProjectFile[]>(existingEntity, projectFiles));
                                unmatchedEntities.Remove(existingEntity);
                            }
                            else
                            {
                                newEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles, neutralResourcesLanguage, duplicateKeyHandling));
                            }
                        }
                    }
                }
            }

            try
            {
                /*
                            Load();
                            await Task.Delay(1).ConfigureAwait(true); 
                /*/
                await Task.Run(Load).ConfigureAwait(true);
                //*/
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                ResourceEntities.Clear();
                Tracer.TraceError(ex.ToString());
                return true;
            }

            ResourceEntities.RemoveRange(unmatchedEntities);
            ResourceEntities.AddRange(newEntities);
            var hasChanged = entitiesToUpdate.Aggregate(false, (current, item) => current | item.Item1.Update(item.Item2, neutralResourcesLanguage, duplicateKeyHandling));

            hasChanged |= unmatchedEntities.Any() || newEntities.Any();

            return hasChanged;
        }

        internal void OnLanguageAdded(ResourceLanguage language, ProjectFile projectFile)
        {
            var cultureKey = language.CultureKey;

            if (!Cultures.Contains(cultureKey))
            {
                Cultures.Add(cultureKey);
            }

            LanguageAdded?.Invoke(this, new LanguageEventArgs(language));
            OnProjectFileSaved(language, projectFile);
        }

        [SuppressPropertyChangedWarnings]
        internal void OnLanguageChanged(ResourceLanguage language)
        {
            LanguageChanged?.Invoke(this, new LanguageEventArgs(language));
        }

        public void OnProjectFileSaved(ResourceLanguage language, ProjectFile projectFile)
        {
            ProjectFileSaved?.Invoke(this, new ProjectFileEventArgs(language, projectFile));
        }

        public void LoadSnapshot(string? value)
        {
            ResourceEntities.LoadSnapshot(value);

            _snapshot = value;
        }

        public string CreateSnapshot()
        {
            return _snapshot = ResourceEntities.CreateSnapshot();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}