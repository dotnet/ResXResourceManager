namespace tomenglertde.ResXManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Main)]
    internal class MainViewModel : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private readonly ITracer _tracer;
        private readonly SourceFilesProvider _sourceFilesProvider;
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public MainViewModel(ResourceManager resourceManager, Configuration configuration, ITracer tracer, SourceFilesProvider sourceFilesProvider)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(tracer != null);
            Contract.Requires(sourceFilesProvider != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
            _tracer = tracer;
            _sourceFilesProvider = sourceFilesProvider;

            resourceManager.BeginEditing += ResourceManager_BeginEditing;

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

        public ICommand BrowseCommand
        {
            get
            {
                return new DelegateCommand(Browse);
            }
        }

        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
            }
        }

        public SourceFilesProvider SourceFilesProvider
        {
            get
            {
                Contract.Ensures(Contract.Result<SourceFilesProvider>() != null);
                return _sourceFilesProvider;
            }
        }

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
                _resourceManager.ReloadAndBeginFindCoreReferences();
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }

        private void ResourceManager_BeginEditing(object sender, ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit(ResourceEntity entity, CultureKey cultureKey)
        {
            Contract.Requires(entity != null);

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

                        if (MessageBox.Show(message, "ResX Resource Manager", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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

                        File.WriteAllText(languageFileName, View.Properties.Resources.EmptyResxTemplate);
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

        [Localizable(false)]
        private static string FormatFileNames(IEnumerable<string> lockedFiles)
        {
            Contract.Requires(lockedFiles != null);

            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_sourceFilesProvider != null);
        }
    }

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    class SourceFilesProvider : ObservableObject, ISourceFilesProvider
    {
        private readonly Configuration _configuration;
        private string _folder;

        [ImportingConstructor]
        public SourceFilesProvider(Configuration configuration)
        {
            Contract.Requires(configuration != null);

            _configuration = configuration;
        }

        public string Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                SetProperty(ref _folder, value, () => Folder);
            }
        }


        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = Folder;
                if (string.IsNullOrEmpty(folder))
                    return new ProjectFile[0];

                return new DirectoryInfo(folder).GetAllSourceFiles(_configuration);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_configuration != null);
        }
    }
}
