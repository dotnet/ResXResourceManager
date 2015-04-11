namespace tomenglertde.ResXManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls.Primitives;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Properties;

    using TomsToolbox.Wpf;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ITracer
    {
        private readonly Configuration _configuration = new Configuration();

        public MainWindow()
        {
            InitializeComponent();

            EventManager.RegisterClassHandler(typeof(MainWindow), ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));
        }

        public string Folder
        {
            get { return (string)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }
        /// <summary>
        /// Identifies the Folder dependency property
        /// </summary>
        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register("Folder", typeof(string), typeof(MainWindow));

        internal ResourceManager ViewModel
        {
            get
            {
                return (ResourceManager)DataContext;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            try
            {
                var resourceManager = new ResourceManager();
                resourceManager.BeginEditing += ResourceManager_BeginEditing;
                resourceManager.ReloadRequested += ResourceManager_ReloadRequested;
                DataContext = resourceManager;

                var folder = Settings.StartupFolder;

                if (!string.IsNullOrEmpty(folder))
                {
                    Folder = folder;

                    if (Directory.Exists(folder))
                    {
                        Load();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "CA is wrong about this!")]
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Settings.StartupFolder })
            {
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                Folder = Settings.StartupFolder = dlg.SelectedPath;
                Settings.Save();

                Load();
            }
        }

        private void Load()
        {
            var folder = Folder;
            if (string.IsNullOrEmpty(folder))
                return;

            var resourceManager = (ResourceManager)DataContext;
            if (resourceManager == null)
                return;

            var sourceFiles = new DirectoryInfo(folder).GetAllSourceFiles(_configuration);

            resourceManager.Load(sourceFiles, _configuration);

            if (View.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                CodeReference.BeginFind(resourceManager, sourceFiles, this);
            }
        }

        private static Settings Settings
        {
            get
            {
                return Settings.Default;
            }
        }

        private static void Navigate_Click(object sender, RoutedEventArgs e)
        {
            string url = null;

            var source = e.OriginalSource as FrameworkElement;
            if (source != null)
            {
                var button = source.TryFindAncestorOrSelf<ButtonBase>();
                if (button == null)
                    return;

                url = source.Tag as string;
                if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return;
            }
            else
            {
                var link = e.OriginalSource as System.Windows.Documents.Hyperlink;
                if (link == null)
                    return;

                var navigateUri = link.NavigateUri;
                if (navigateUri == null)
                    return;

                url = navigateUri.ToString();
            }

            Process.Start(url);
        }

        private void ResourceManager_BeginEditing(object sender, ResourceBeginEditingEventArgs e)
        {
            Contract.Requires(sender != null);

            var resourceManager = (ResourceManager)sender;

            if (!CanEdit(resourceManager, e.Entity, e.Culture))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit(ResourceManager resourceManager, ResourceEntity entity, CultureInfo culture)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(entity != null);

            string message;
            var languages = entity.Languages.Where(lang => (culture == null) || culture.Equals(lang.Culture)).ToArray();

            var rootFolder = Folder;
            if (string.IsNullOrEmpty(rootFolder))
                return false;

            if (!languages.Any())
            {
                try
                {
                    // because entity.Languages.Any() => languages can only be empty if language != null!
                    Contract.Assume(culture != null);

                    if (resourceManager.Configuration.ConfirmAddLanguage)
                    {
                        message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProjectHasNoResourceFile, culture.DisplayName);

                        if (MessageBox.Show(message, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex), Title);
                }
            }
            else
            {
                var lockedFiles = languages.Where(l => !l.ProjectFile.IsWritable).Select(l => l.FileName).ToArray();

                if (!lockedFiles.Any())
                    return true;

                message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));
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

        private void ResourceManager_ReloadRequested(object sender, EventArgs e)
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void TraceError(string value)
        {
            Trace.TraceError(value);
        }

        public void WriteLine(string value)
        {
            Trace.TraceInformation(value);
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_configuration != null);
        }
    }
}