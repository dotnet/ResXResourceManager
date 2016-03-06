namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows;

    using Microsoft.Win32;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(ResourceViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ResourceView
    {
        private readonly ResourceManager _resourceManager;

        [ImportingConstructor]
        public ResourceView(ExportProvider exportProvider, ResourceManager resourceManager)
        {
            Contract.Requires(exportProvider != null);

            this.SetExportProvider(exportProvider);

            InitializeComponent();

            _resourceManager = resourceManager;
            _resourceManager.Loaded += ResourceManager_Loaded;

            DataGrid.SetupColumns(_resourceManager);
        }

        private void ResourceManager_Loaded(object sender, EventArgs e)
        {
            DataGrid.SetupColumns(_resourceManager);
        }

        private void AddLanguage_Click(object sender, RoutedEventArgs e)
        {
            var exisitingCultures = _resourceManager.CultureKeys
                .Select(c => c.Culture)
                .Where(c => c != null);

            var inputBox = new LanguageSelectionBox(exisitingCultures)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (!inputBox.ShowDialog().GetValueOrDefault())
                return;

            WaitCursor.Start(this);

            var culture = inputBox.SelectedLanguage;

            DataGrid.CreateNewLanguageColumn(_resourceManager, culture);

            _resourceManager.LanguageAdded(culture);
        }

        private void CreateSnapshotCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".snapshot",
                Filter = "Snapshots|*.snapshot|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Today.ToShortDateString().ReplaceInvalidFileNameChars('_')
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void LoadSnapshotCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
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

        private void ExportExcelCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Today.ToShortDateString().ReplaceInvalidFileNameChars('_')
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = new ExportParameters(dlg.FileName, e.Parameter as IResourceScope);

            WaitCursor.Start(this);
        }

        private void ImportExcelCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void DeleteCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmDeleteItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CutCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmCutItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CommandConverter_Error(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();

            if (ex == null)
                return;

            var text = (ex is ImportException) ? ex.Message : ex.ToString();

            MessageBox.Show(text, Properties.Resources.Title);
        }

        private class ExportParameters : IExportParameters
        {
            public ExportParameters(string fileName, IResourceScope scope)
            {
                FileName = fileName;
                Scope = scope;
            }

            public IResourceScope Scope
            {
                get;
                private set;
            }

            public string FileName
            {
                get;
                private set;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(DataGrid != null);
        }
    }
}
