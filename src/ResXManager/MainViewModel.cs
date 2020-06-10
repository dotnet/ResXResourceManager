namespace ResXManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using Microsoft.WindowsAPICodePack.Dialogs;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Properties;
    using ResXManager.View.Tools;
    using ResXManager.View.Visuals;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.Mef;

    [VisualCompositionExport(RegionId.Main)]
    internal class MainViewModel : ObservableObject
    {
        private readonly ITracer _tracer;
        private readonly Configuration _configuration;
        private readonly ResourceViewModel _resourceViewModel;
        private readonly PowerShellProcessor _powerShell;

        [ImportingConstructor]
        public MainViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration, [NotNull] ResourceViewModel resourceViewModel, [NotNull] ITracer tracer, [NotNull] SourceFilesProvider sourceFilesProvider)
        {
            ResourceManager = resourceManager;
            _configuration = configuration;
            _resourceViewModel = resourceViewModel;
            _tracer = tracer;
            _powerShell = new PowerShellProcessor(tracer);
            SourceFilesProvider = sourceFilesProvider;

            resourceManager.BeginEditing += ResourceManager_BeginEditing;
            resourceManager.Reloading += ResourceManager_Reloading;
            resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;

            try
            {
                var folder = Settings.Default.StartupFolder;

                if (string.IsNullOrEmpty(folder))
                    return;

                SourceFilesProvider.SolutionFolder = folder;

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

        private async void ResourceManager_ProjectFileSaved(object sender, ProjectFileEventArgs e)
        {
            var solutionFolder = SourceFilesProvider.SolutionFolder;
            if (string.IsNullOrEmpty(solutionFolder))
                return;

            var scriptFile = Path.Combine(solutionFolder, "Resources.ps1");
            if (!File.Exists(scriptFile))
                return;

            await _powerShell.Run(solutionFolder, scriptFile).ConfigureAwait(false);
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

                    SourceFilesProvider.SolutionFolder = settings.StartupFolder = dlg.FileName;

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

                SourceFilesProvider.SolutionFolder = Settings.Default.StartupFolder = dlg.SelectedPath;

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

            var rootFolder = SourceFilesProvider.SolutionFolder;
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

        private class PowerShellProcessor
        {
            private readonly ITracer _tracer;

            [CanBeNull]
            private readonly string _powerShellPath = FindInPath("PowerShell.exe");

            private bool _isConverting;
            private bool _isConversionRequestPending;

            // ReSharper disable once AssignNullToNotNullAttribute
            private readonly string _workingDirectory = Path.GetDirectoryName(typeof(Scripting.Host).Assembly.Location);

            public PowerShellProcessor(ITracer tracer)
            {
                _tracer = tracer;
            }

            public async Task Run(string solutionFolder, string scriptFile)
            {
                if (_powerShellPath == null)
                    return;

                if (_isConverting)
                {
                    _isConversionRequestPending = true;
                    return;
                }

                _isConverting = true;

                while (true)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            var startInfo = new ProcessStartInfo()
                            {
                                FileName = _powerShellPath,
                                Arguments = $@"-NoProfile -ExecutionPolicy Bypass -file ""{scriptFile}"" -solutionDir ""{solutionFolder}""",
                                CreateNoWindow = true,
                                WorkingDirectory = _workingDirectory,
                                UseShellExecute = false,
                                RedirectStandardError = true,
                                RedirectStandardOutput = true
                            };

                            var process = Process.Start(startInfo);
                            if (process == null)
                                return;

                            process.StandardError.ReadToEndAsync().ContinueWith(task =>
                            {
                                var errors = task.Result;
                                if (!string.IsNullOrEmpty(errors))
                                {
                                    _tracer.TraceError("PowerShell: " + errors);
                                }
                            });

                            process.StandardOutput.ReadToEndAsync().ContinueWith(task =>
                            {
                                var value = task.Result;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    _tracer.WriteLine("PowerShell: " + value);
                                }
                            });

                            if (!process.WaitForExit(10000))
                            {
                                process.Kill();
                                _tracer.TraceError("Script execution timed out: " + scriptFile);
                            }
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _tracer.TraceError(ex.ToString());
                    }

                    if (!_isConversionRequestPending)
                    {
                        _isConverting = false;
                        return;
                    }

                    _isConversionRequestPending = false;
                }
            }

            [CanBeNull]
            private static string FindInPath(string fileName)
            {
                return Environment.GetEnvironmentVariable("PATH")?
                    .Split(';')
                    .Select(item => Path.Combine(item.Trim(), fileName))
                    .FirstOrDefault(File.Exists);
            }
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
        public string SolutionFolder { get; set; }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = SolutionFolder;
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
