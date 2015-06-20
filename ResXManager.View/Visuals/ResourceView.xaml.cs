namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Threading;

    using Microsoft.Win32;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    public partial class ResourceView
    {
        public ResourceView()
        {
            InitializeComponent();

            BindingOperations.SetBinding(this, EntityFilterProperty, new Binding("DataContext.EntityFilter") { Source = this });
        }

        private static readonly DependencyProperty EntityFilterProperty =
            DependencyProperty.Register("EntityFilter", typeof(string), typeof(ResourceView), new FrameworkPropertyMetadata(null, (sender, e) => Settings.Default.ResourceFilter = (string)e.NewValue));

        private ResourceManager ViewModel
        {
            get
            {
                return (ResourceManager)DataContext;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            this.BeginInvoke(DispatcherPriority.Background, () => ListBox.SelectAll());
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DataContextProperty)
            {
                DataContext_Changed(e);
            }
        }

        private void DataContext_Changed(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as ResourceManager;
            if (oldValue != null)
            {
                oldValue.Loaded -= ResourceManager_Loaded;
            }

            var newValue = e.NewValue as ResourceManager;
            if (newValue != null)
            {
                newValue.Loaded += ResourceManager_Loaded;
                newValue.EntityFilter = Settings.Default.ResourceFilter;
            }
        }

        private void NeutralLanguage_Click(object sender, RoutedEventArgs e)
        {
            Contract.Requires(sender != null);

            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            viewModel.Configuration.NeutralResourcesLanguage = (CultureInfo)(((MenuItem)sender).DataContext);
        }

        private void ResourceManager_Loaded(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            DataGrid.SetupColumns(viewModel.CultureKeys);
        }

        private void AddLanguage_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            var exisitingCultures = viewModel.CultureKeys
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

            DataGrid.CreateNewLanguageColumn(viewModel, culture);

            viewModel.LanguageAdded(culture);
        }

        private void ExportExcelCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0
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
            Contract.Invariant(DataGrid != null);
        }
    }
}
