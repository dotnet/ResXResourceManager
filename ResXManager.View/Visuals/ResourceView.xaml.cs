namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
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
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Converters;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Desktop;

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

        public double TextFontSize
        {
            get { return this.GetValue<double>(TextFontSizeProperty); }
            set { SetValue(TextFontSizeProperty, value); }
        }
        public static readonly DependencyProperty TextFontSizeProperty =
            DependencyProperty.RegisterAttached("TextFontSize", typeof(double), typeof(ResourceView), new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.Inherits));

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

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => ListBox.SelectAll());
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
            var inputBox = new InputBox
            {
                Title = Properties.Resources.Title,
                Prompt = Properties.Resources.NewLanguageIdPrompt,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            var cultureNames = viewModel.CultureKeys
                .Select(c => c.Culture)
                .Where(c => c != null)
                .Select(c => c.ToString()).ToArray();

            inputBox.TextChanged += (_, args) =>
                inputBox.IsInputValid = !cultureNames.Contains(args.Text, StringComparer.OrdinalIgnoreCase) && ResourceManager.IsValidLanguageName(args.Text);

            if (!inputBox.ShowDialog().GetValueOrDefault())
                return;

            WaitCursor.Start(this);

            var culture = new CultureInfo(inputBox.Text);

            DataGrid.CreateNewLanguageColumn(viewModel, culture);

            viewModel.LanguageAdded(culture);
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

            if (dlg.ShowDialog() != true)
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;
        }

        private void CommandConverter_Error(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();

            if (ex == null)
                return;

            var text = (ex is ImportException) ? ex.Message : ex.ToString();

            MessageBox.Show(text, Properties.Resources.Title);
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(DataGrid != null);
        }
    }
}
