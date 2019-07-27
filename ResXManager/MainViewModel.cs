namespace tomenglertde.ResXManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Properties;
    using tomenglertde.ResXManager.View.Tools;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.Mef;

    [VisualCompositionExport(RegionId.Main)]
    internal class MainViewModel : ObservableObject
    {
        [NotNull]
        private readonly ITracer _tracer;
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;

        [ImportingConstructor]
        public MainViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration, [NotNull] ResourceViewModel resourceViewModel, [NotNull] ITracer tracer, [NotNull] SourceFilesProvider sourceFilesProvider)
        {
            ResourceManager = resourceManager;
            _configuration = configuration;
            _resourceViewModel = resourceViewModel;
            _tracer = tracer;
            SourceFilesProvider = sourceFilesProvider;

            resourceManager.BeginEditing += ResourceManager_BeginEditing;
            resourceManager.Reloading += ResourceManager_Reloading;

            try
            {
                var folder = Settings.Default.StartupFolder;

                if (string.IsNullOrEmpty(folder))
                    return;

                SourceFilesProvider.Folder = folder;

                if (Directory.Exists(folder))
                {
                    Load();
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }

        [NotNull]
        public ICommand BrowseCommand => new DelegateCommand(Browse);

        [NotNull]
        public ResourceManager ResourceManager { get; }

        [NotNull]
        public SourceFilesProvider SourceFilesProvider { get; }

        private void Browse()
        {
            var settings = Settings.Default;

            try
            {
                using (var dlg = new CommonOpenFileDialog { IsFolderPicker = true, InitialDirectory = settings.StartupFolder, EnsurePathExists = true })
                {
                    if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                        return;

                    SourceFilesProvider.Folder = settings.StartupFolder = dlg.FileName;

                    Load();
                    return;
                }
            }
            catch (NotSupportedException)
            {
                // CommonOpenFileDialog not supported on current platform.
            }

            using (var dlg = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Settings.Default.StartupFolder })
            {
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                SourceFilesProvider.Folder = Settings.Default.StartupFolder = dlg.SelectedPath;

                Load();
            }
        }

        private void Load()
        {
            try
            {
                _resourceViewModel.Reload();
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }

        private void ResourceManager_Reloading([NotNull] object sender, [NotNull] CancelEventArgs e)
        {
            if (!ResourceManager.HasChanges)
                return;

            if (MessageBoxResult.Yes == MessageBox.Show(Resources.WarningUnsavedChanges, View.Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No))
                return;

            e.Cancel = true;
        }

        private void ResourceManager_BeginEditing([NotNull] object sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, [CanBeNull] CultureKey cultureKey)
        {
            string message;
            var languages = entity.Languages.Where(lang => (cultureKey == null) || cultureKey.Equals(lang.CultureKey)).ToArray();

            var rootFolder = SourceFilesProvider.Folder;
            if (string.IsNullOrEmpty(rootFolder))
                return false;

            if (!languages.Any())
            {
                try
                {
                    var culture = cultureKey?.Culture;

                    if (culture == null)
                        return false; // no neutral culture => this should never happen.

                    if (_configuration.ConfirmAddLanguageFile)
                    {
                        message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasNoResourceFile, culture.DisplayName);

                        if (MessageBox.Show(message, View.Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            return false;
                    }

                    var neutralLanguage = entity.Languages.FirstOrDefault();
                    if (neutralLanguage == null)
                        return false;

                    var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(culture);

                    if (!File.Exists(languageFileName))
                    {
                        var directoryName = Path.GetDirectoryName(languageFileName);
                        if (!string.IsNullOrEmpty(directoryName))
                            Directory.CreateDirectory(directoryName);

                        File.WriteAllText(languageFileName, Model.Properties.Resources.EmptyResxTemplate);
                    }

                    entity.AddLanguage(new ProjectFile(languageFileName, rootFolder, entity.ProjectName, null));
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex), "ResX Resource Manager");
                }
            }
            else
            {
                var lockedFiles = languages.Where(l => !l.ProjectFile.IsWritable).Select(l => l.FileName).ToArray();

                if (!lockedFiles.Any())
                    return true;

                message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));
                MessageBox.Show(message);
            }

            return false;
        }

        [NotNull]
        [Localizable(false)]
        private static string FormatFileNames([NotNull][ItemNotNull] IEnumerable<string> lockedFiles)
        {
            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }
    }

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    internal class SourceFilesProvider : ObservableObject, ISourceFilesProvider
    {
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        [ImportingConstructor]
        public SourceFilesProvider([NotNull] Configuration configuration, [NotNull] PerformanceTracer performanceTracer)
        {
            _configuration = configuration;
            _performanceTracer = performanceTracer;
        }

        [CanBeNull]
        public string Folder { get; set; }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = Folder;
                if (string.IsNullOrEmpty(folder))
                    return Array.Empty<ProjectFile>();

                using (_performanceTracer.Start("Enumerate source files"))
                {
                    return new DirectoryInfo(folder).GetAllSourceFiles(new FileFilter(_configuration));
                }
            }
        }

        public void Invalidate()
        {
        }
    }
}
