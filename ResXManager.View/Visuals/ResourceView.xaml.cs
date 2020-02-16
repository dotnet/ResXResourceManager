namespace ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using Microsoft.Win32;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Tools;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.Mef;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(ResourceViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ResourceView
    {
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;
        [NotNull]
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public ResourceView([NotNull] IExportProvider exportProvider)
        {
            _resourceManager = exportProvider.GetExportedValue<ResourceManager>();
            _resourceViewModel = exportProvider.GetExportedValue<ResourceViewModel>();
            _configuration = exportProvider.GetExportedValue<Configuration>();
            _tracer = exportProvider.GetExportedValue<ITracer>();
            _resourceViewModel.ClearFiltersRequest += ResourceViewModel_ClearFiltersRequest;

            try
            {
                this.SetExportProvider(exportProvider);

                _resourceManager.Loaded += ResourceManager_Loaded;

                InitializeComponent();

                DataGrid?.SetupColumns(_resourceManager, _resourceViewModel, _configuration);
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }

        private void ResourceViewModel_ClearFiltersRequest(object sender, [NotNull] ResourceTableEntryEventArgs e)
        {
            var filter = DataGrid.Items.Filter;

            if (filter == null)
                return;

            if (filter(e.Entry))
                return;

            DataGrid.GetFilter().Clear();
        }

        private void ResourceManager_Loaded([NotNull] object sender, [NotNull] EventArgs e)
        {
            DataGrid?.SetupColumns(_resourceManager, _resourceViewModel, _configuration);
        }

        private void AddLanguage_Click([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            var existingCultures = _resourceManager.Cultures
                .Select(c => c.Culture)
                .Where(c => c != null);

            var languageSelection = new LanguageSelectionBoxViewModel(existingCultures);

            var window = Window.GetWindow(this);

            if (!ConfirmationDialog.Show(this.GetExportProvider(), languageSelection, Properties.Resources.Title, window).GetValueOrDefault())
                return;

            WaitCursor.Start(this);

            var culture = languageSelection.SelectedLanguage;

            DataGrid.CreateNewLanguageColumn(_configuration, culture);

            if (!_configuration.AutoCreateNewLanguageFiles)
                return;

            if (!_resourceManager.ResourceEntities.All(resourceEntity => _resourceManager.CanEdit(resourceEntity, culture)))
            {
                // nothing left to do, message already shown.
            }
        }

        private void CreateSnapshotCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".snapshot",
                Filter = "Snapshots|*.snapshot|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Today.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture)
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void LoadSnapshotCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".snapshot",
                Filter = "Snapshots|*.snapshot|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void ExportExcelCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm", CultureInfo.InvariantCulture)
            };

            if (_configuration.ExcelExportMode == ExcelExportMode.Text)
            {
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text files|*.txt|CSV files|*.csv|All Files|*.*";
            }

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = new ExportParameters(dlg.FileName, e.Parameter as IResourceScope);

            WaitCursor.Start(this);
        }

        private void ImportExcelCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".xlsx",
                Filter = "Exported files|*.xlsx;*.txt;*.csv|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void DeleteCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmDeleteItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CutCommandConverter_Executing([NotNull] object sender, [NotNull] ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmCutItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CommandConverter_Error([NotNull] object sender, [NotNull] ErrorEventArgs e)
        {
            var ex = e.GetException();

            if (ex == null)
                return;

            MessageBox.Show(ex.Message, Properties.Resources.Title);

            if (ex is ImportException)
                return;

            _tracer.TraceError(ex.ToString());
        }

        private class ExportParameters : IExportParameters
        {
            public ExportParameters([CanBeNull] string fileName, [CanBeNull] IResourceScope scope)
            {
                FileName = fileName;
                Scope = scope;
            }

            [CanBeNull]
            public IResourceScope Scope
            {
                get;
            }

            [CanBeNull]
            public string FileName
            {
                get;
            }
        }
    }
}
