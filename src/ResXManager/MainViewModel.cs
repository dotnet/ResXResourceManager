namespace ResXManager;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Microsoft.WindowsAPICodePack.Dialogs;

using PropertyChanged;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.Properties;
using ResXManager.View.Tools;
using ResXManager.View.Visuals;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition.AttributedModel;

[VisualCompositionExport(RegionId.Main)]
[Shared]
internal class MainViewModel : ObservableObject
{
    private readonly ITracer _tracer;

    [ImportingConstructor]
    public MainViewModel(ResourceManager resourceManager, IStandaloneConfiguration configuration, ResourceViewModel resourceViewModel, ITracer tracer, SourceFilesProvider sourceFilesProvider)
    {
        ResourceManager = resourceManager;
        ResourceViewModel = resourceViewModel;
        _tracer = tracer;
        Configuration = configuration;
        SourceFilesProvider = sourceFilesProvider;

        resourceManager.BeginEditing += ResourceManager_BeginEditing;
        resourceManager.Reloading += ResourceManager_Reloading;

        try
        {
            var folder = Clipboard.GetText()?.Trim();

            if (folder.IsNullOrEmpty() || !Directory.Exists(folder))
            {
                folder = Settings.Default.StartupFolder;

                if (folder.IsNullOrEmpty() || !Directory.Exists(folder))
                    return;
            }

            SourceFilesProvider.SolutionFolder = folder;

            Load();
        }
        catch (Exception ex)
        {
            _tracer.TraceError(ex.ToString());
            MessageBox.Show(ex.Message);
        }
    }

    public ICommand BrowseCommand => new DelegateCommand(Browse);

    public ICommand CloseSolutionCommand => new DelegateCommand(CloseSolution);

    public ICommand SetSolutionFolderCommand => new DelegateCommand<string>(SetSolutionFolder);

    public ResourceManager ResourceManager { get; }

    public ResourceViewModel ResourceViewModel { get; }

    public SourceFilesProvider SourceFilesProvider { get; }

    public IStandaloneConfiguration Configuration { get; }

    private void Browse()
    {
        var settings = Settings.Default;

        try
        {
            using var commonDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = settings.StartupFolder,
                EnsurePathExists = true
            };

            if (commonDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            SourceFilesProvider.SolutionFolder = commonDialog.FileName;
            Load();
            return;
        }
        catch (NotSupportedException)
        {
            // CommonOpenFileDialog not supported on current platform.
        }

        using var browserDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            SelectedPath = Settings.Default.StartupFolder
        };
        
        if (browserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        SourceFilesProvider.SolutionFolder = browserDialog.SelectedPath;
        Load();
    }

    private void CloseSolution()
    {
        if (ResourceManager.HasChanges)
        {
            if (MessageBoxResult.Yes == MessageBox.Show(View.Properties.Resources.WarningUnsavedChanges, View.Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No))
                return;
        }

        SetSolutionFolder(string.Empty);
    }

    private void SetSolutionFolder(string folder)
    {
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            return;

        SourceFilesProvider.SolutionFolder = folder;
        Load();
    }

    private async void Load()
    {
        try
        {
            await ResourceViewModel.ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _tracer.TraceError(ex.ToString());
            MessageBox.Show(ex.Message);
        }
    }

    private void ResourceManager_Reloading(object? sender, CancelEventArgs e)
    {
        if (!ResourceManager.HasChanges)
            return;

        if (MessageBoxResult.Yes == MessageBox.Show(View.Properties.Resources.WarningUnsavedChanges, View.Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No))
            return;

        e.Cancel = true;
    }

    private void ResourceManager_BeginEditing(object? sender, ResourceBeginEditingEventArgs e)
    {
        if (!CanEdit(e.Entity, e.CultureKey))
        {
            e.Cancel = true;
        }
    }

    private bool CanEdit(ResourceEntity entity, CultureKey? cultureKey)
    {
        string message;
        var languages = entity.Languages.Where(lang => (cultureKey == null) || cultureKey.Equals(lang.CultureKey)).ToArray();

        var rootFolder = SourceFilesProvider.SolutionFolder;
        if (rootFolder.IsNullOrEmpty())
            return false;

        if (!languages.Any())
        {
            try
            {
                var culture = cultureKey?.Culture;

                if (culture == null)
                    return false; // no neutral culture => this should never happen.

                if (Configuration.ConfirmAddLanguageFile)
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
                    if (!directoryName.IsNullOrEmpty())
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

    [Localizable(false)]
    private static string FormatFileNames(IEnumerable<string> lockedFiles)
    {
        return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
    }
}

[Export]
[Export(typeof(ISourceFilesProvider))]
[Shared]
internal class SourceFilesProvider : ObservableObject, ISourceFilesProvider
{
    private readonly IStandaloneConfiguration _configuration;
    private readonly PerformanceTracer _performanceTracer;

    [ImportingConstructor]
    public SourceFilesProvider(IStandaloneConfiguration configuration, PerformanceTracer performanceTracer)
    {
        _configuration = configuration;
        _performanceTracer = performanceTracer;
    }

    [OnChangedMethod(nameof(OnSolutionFolderChanged))]
    public string? SolutionFolder { get; set; }

    private void OnSolutionFolderChanged()
    {
        var solutionFolder = SolutionFolder;

        if (!Directory.Exists(solutionFolder)) 
            return;

        Settings.Default.StartupFolder = solutionFolder;
    }

    public async Task<IList<ProjectFile>> GetSourceFilesAsync(CancellationToken? cancellationToken)
    {
        var folder = SolutionFolder;
        if (folder.IsNullOrEmpty())
            return Array.Empty<ProjectFile>();

        using (_performanceTracer.Start("Enumerate source files"))
        {
            var fileFilter = new FileFilter(_configuration);
            var directoryInfo = new DirectoryInfo(folder);

            return await Task.Run(() => directoryInfo.GetAllSourceFiles(fileFilter, cancellationToken), cancellationToken ?? new CancellationToken()).ConfigureAwait(false);
        }
    }
}
