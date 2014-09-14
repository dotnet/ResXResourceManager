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
    using System.Windows.Forms;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Properties;
    using MessageBox = System.Windows.MessageBox;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ContractVerification(false)] // Too many warnings from generated code.
    public partial class MainWindow : ITracer
    {
        public MainWindow()
        {
            InitializeComponent();
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
            DependencyProperty.Register("Folder", typeof (string), typeof (MainWindow));

        internal ResourceManager ViewModel
        {
            get
            {
                return (ResourceManager) DataContext;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            try
            {
                if (!string.IsNullOrEmpty(Settings.StartupFolder))
                {
                    Folder = Settings.StartupFolder;
                }

                if ((Folder != null) && Directory.Exists(Folder))
                {
                    Load();
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
            using (var dlg = new FolderBrowserDialog {SelectedPath = Settings.StartupFolder})
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
            var sourceFileExtensions = Settings.SourceFiles
                .Split(';')
                .Select(s => s.Trim())
                .ToArray();

            var sourceFiles = new DirectoryInfo(Folder).GetAllSourceFiles(file => sourceFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase));

            ViewModel.Load(sourceFiles);

            if (View.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                CodeReference.BeginFind(ViewModel.ResourceEntities, sourceFiles, this);
            }
        }

        private static Settings Settings
        {
            get
            {
                return Settings.Default;
            }
        }

        private void ResourceView_NavigateClick(object sender, RoutedEventArgs e)
        {
            var source = e.Source as FrameworkElement;
            if (source == null)
                return;

            var url = source.Tag as string;
            if (string.IsNullOrEmpty(url))
                return;

            Process.Start(url);
        }

        private void ResourceView_BeginEditing(object sender, ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.Language))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit(ResourceEntity entity, CultureInfo language)
        {
            Contract.Requires(entity != null);

            string message;
            var languages = entity.Languages.Where(lang => (language == null) || language.Equals(lang.Culture)).ToArray();

            if (!languages.Any())
            {
                try
                {
                    // because entity.Languages.Any() => languages can only be empty if language != null!
                    Contract.Assume(language != null);

                    message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProjectHasNoResourceFile, language.DisplayName);

                    if (MessageBox.Show(message, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return false;

                    var neutralLanguage = entity.Languages.First();

                    var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(language);

                    if (File.Exists(languageFileName))
                    {
                        if (MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.FileExistsPrompt, languageFileName), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            return false;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(languageFileName));

                    File.WriteAllText(languageFileName, View.Properties.Resources.EmptyResxTemplate);

                    entity.AddLanguage(new ProjectFile(languageFileName, Folder, entity.ProjectName, null));

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex), Title);
                }
            }
            else
            {
                var lockedFiles = languages.Where(l => !l.IsWritable).Select(l => l.FileName).ToArray();

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
            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }

        private void ResourceView_ReloadRequested(object sender, EventArgs e)
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
    }
}