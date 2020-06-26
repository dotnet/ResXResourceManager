namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Media;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.Visuals;
    using ResXManager.VSIX.Visuals;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.XamlExtensions;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("79664857-03bf-4bca-aa54-ec998b3328f8")]
    public sealed class MyToolWindow : ToolWindowPane
    {
        [NotNull]
        private readonly ITracer _tracer;

        [NotNull]
        private readonly Configuration _configuration;

        [NotNull]
        private readonly IExportProvider _exportProvider;

        [NotNull]
        private readonly ContentControl _contentWrapper = new ContentControl
        {
            Focusable = false, Content = new Border { Background = Brushes.Red }
        };

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow()
            : base(null)
        {
            // Set the window title reading it from the resources.
            Caption = Resources.ToolWindowTitle;

            // Set the image that will appear on the tab of the window frame when docked with an other window.
            // The resource ID correspond to the one defined in the resx file while the Index is the offset in the bitmap strip.
            // Each image in the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;

            var exportProvider = VsPackage.Instance.ExportProvider;
            _tracer = exportProvider.GetExportedValue<ITracer>();
            _configuration = exportProvider.GetExportedValue<Configuration>();

            exportProvider.GetExportedValue<ResourceManager>().BeginEditing += ResourceManager_BeginEditing;

            _exportProvider = exportProvider;

            VisualComposition.Error += VisualComposition_Error;
            _contentWrapper.Loaded += ContentWrapper_Loaded;
            _contentWrapper.Unloaded += ContentWrapper_Unloaded;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                _tracer.WriteLine(Resources.IntroMessage);

                var executingAssembly = Assembly.GetExecutingAssembly();
                var folder = Path.GetDirectoryName(executingAssembly.Location);

                // ReSharper disable once AssignNullToNotNullAttribute
                _tracer.WriteLine(Resources.AssemblyLocation, folder);
                _tracer.WriteLine(Resources.Version, new AssemblyName(executingAssembly.FullName).Version);
                _tracer.WriteLine(".NET Framework Version: {0} (https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed)", FrameworkVersion());

                const string switchName = @"Switch.System.Windows.Baml2006.AppendLocalAssemblyVersionForSourceUri";
                AppContext.TryGetSwitch(switchName, out var isEnabled);
                _tracer.WriteLine("{0}={1} (https://github.com/Microsoft/dotnet/blob/master/releases/net472/dotnet472-changes.md#wpf)", switchName, isEnabled);

                EventManager.RegisterClassHandler(typeof(VsixShellView), ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));

                // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
                // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
                // the object returned by the Content property.
                Content = _contentWrapper;
            }
            catch (Exception ex)
            {
                _tracer.TraceError("MyToolWindow OnCreate failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
        }

        private void ContentWrapper_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var view = _exportProvider.GetExportedValue<VsixShellView>();

                _contentWrapper.Content = view;

                Dte.SetFontSize(view);
            }
            catch (Exception ex)
            {
                _tracer.TraceError("ContentWrapper_Loaded failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
        }

        private void ContentWrapper_Unloaded(object sender, RoutedEventArgs e)
        {
            _contentWrapper.Content = null;
        }

        [NotNull]
        private EnvDTE.DTE Dte
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                return dte;
            }
        }

        private static void Navigate_Click(object? sender, [NotNull] RoutedEventArgs e)
        {
            string? url;

            if (e.OriginalSource is FrameworkElement source)
            {
                var button = source.TryFindAncestorOrSelf<ButtonBase>();
                if (button == null)
                    return;

                url = source.Tag as string;
                if (url?.StartsWith(@"http", StringComparison.OrdinalIgnoreCase) != true)
                    return;
            }
            else
            {
                var link = e.OriginalSource as Hyperlink;

                var navigateUri = link?.NavigateUri;
                if (navigateUri == null)
                    return;

                url = navigateUri.ToString();
            }

            CreateWebBrowser(url);
        }

        [Localizable(false)]
        private static void CreateWebBrowser([NotNull] string url)
        {
            Process.Start(url);
        }

        private void ResourceManager_BeginEditing(object? sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, CultureKey? cultureKey)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var languages = entity.Languages.Where(lang => (cultureKey == null) || cultureKey.Equals(lang.CultureKey)).ToArray();

            if (!languages.Any())
            {
                try
                {
                    var culture = cultureKey?.Culture;

                    if (culture == null)
                        return false; // no neutral culture => this should never happen.

                    return AddLanguage(entity, culture);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex), Resources.ToolWindowTitle);
                }
            }

            var alreadyOpenItems = GetLanguagesOpenedInAnotherEditor(languages);

            string message;

            if (alreadyOpenItems.Any())
            {
                message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorOpenFilesInEditor, FormatFileNames(alreadyOpenItems.Select(item => item.Item1)));
                MessageBox.Show(message, Resources.ToolWindowTitle);

                ActivateWindow(alreadyOpenItems.Select(item => item.Item2).FirstOrDefault());

                return false;
            }

            // if file is not read only, assume file is either 
            // - already checked out
            // - not under source control
            // - or does not need special SCM handling (e.g. TFS local workspace)
            var lockedFiles = GetLockedFiles(languages);

            if (!lockedFiles.Any())
                return true;

            if (!QueryEditFiles(lockedFiles))
                return false;

            // if file is not under source control, we get an OK from QueryEditFiles even if the file is read only, so we have to test again:
            lockedFiles = GetLockedFiles(languages);

            if (!lockedFiles.Any())
                return true;

            message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));
            MessageBox.Show(message, Resources.ToolWindowTitle);
            return false;
        }

        private static void ActivateWindow(EnvDTE.Window? window)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                window?.Activate();
            }
            catch
            {
                // Something is wrong with the window, we can't do anything about this...
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [NotNull, ItemNotNull]
        private Tuple<string, EnvDTE.Window>[] GetLanguagesOpenedInAnotherEditor([NotNull, ItemNotNull] IEnumerable<ResourceLanguage> languages)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable VSTHRD010 // Accessing ... should only be done on the main thread.
                var openDocuments = Dte.Windows?
                    .OfType<EnvDTE.Window>()
                    .Where(window => window.Visible && (window.Document != null))
                    .ToDictionary(window => window.Document);

                var items = from l in languages
                            let file = l.FileName
                            let projectFile = l.ProjectFile as DteProjectFile
                            let documents = projectFile?.ProjectItems.Select(item => item.TryGetDocument()).Where(doc => doc != null)
                            let window = documents?.Select(doc => openDocuments?.GetValueOrDefault(doc)).FirstOrDefault(win => win != null)
                            where window != null
                            select Tuple.Create(file, window);
#pragma warning restore VSTHRD010 // Accessing ... should only be done on the main thread.

                return items.ToArray();
            }
            catch
            {
                return Array.Empty<Tuple<string, EnvDTE.Window>>();
            }
        }

        private bool QueryEditFiles([NotNull] [ItemNotNull] string[] lockedFiles)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var service = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));
            if (service != null)
            {
                if ((0 != service.QueryEditFiles(0, lockedFiles.Length, lockedFiles, null, null, out var editVerdict, out _))
                    || (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    return false;
                }
            }

            return true;
        }

        [NotNull]
        [ItemNotNull]
        private static string[] GetLockedFiles([NotNull] [ItemNotNull] IEnumerable<ResourceLanguage> languages)
        {
            return languages.Where(l => !l.ProjectFile.IsWritable)
                .Select(l => l.FileName)
                .ToArray();
        }

        private bool AddLanguage([NotNull] ResourceEntity entity, [NotNull] CultureInfo culture)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var resourceLanguages = entity.Languages;
            if (!resourceLanguages.Any())
                return false;

            if (_configuration.ConfirmAddLanguageFile)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasNoResourceFile, culture.DisplayName);

                if (MessageBox.Show(message, Resources.ToolWindowTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return false;
            }

            var neutralLanguage = resourceLanguages.First();
            var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(culture);

            if (!File.Exists(languageFileName))
            {
                var directoryName = Path.GetDirectoryName(languageFileName);
                if (!string.IsNullOrEmpty(directoryName))
                    Directory.CreateDirectory(directoryName);

                File.WriteAllText(languageFileName, Model.Properties.Resources.EmptyResxTemplate);
            }

            AddProjectItems(entity, neutralLanguage, languageFileName);

            return true;
        }

        private void AddProjectItems([NotNull] ResourceEntity entity, [NotNull] ResourceLanguage neutralLanguage, [NotNull] string languageFileName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            DteProjectFile? projectFile = null;

            var projectItems = (neutralLanguage.ProjectFile as DteProjectFile)?.ProjectItems;
            if (projectItems == null)
            {
                entity.AddLanguage(new ProjectFile(languageFileName, entity.Container.SolutionFolder ?? string.Empty, entity.ProjectName, null));
                return;
            }

            foreach (var neutralLanguageProjectItem in projectItems)
            {
                var collection = neutralLanguageProjectItem.Collection;
                var projectItem = collection.AddFromFile(languageFileName);
                var containingProject = projectItem.ContainingProject;
                var projectName = containingProject.Name;
                if (projectFile == null)
                {
                    var solution = _exportProvider.GetExportedValue<DteSolution>();

                    projectFile = new DteProjectFile(solution, solution.SolutionFolder, languageFileName, projectName, containingProject.UniqueName, projectItem);
                }
                else
                {
                    projectFile.AddProject(projectName, projectItem);
                }
            }

            if (projectFile != null)
            {
                entity.AddLanguage(projectFile);
            }
        }

        [NotNull]
        [Localizable(false)]
        private static string FormatFileNames([NotNull] [ItemNotNull] IEnumerable<string> lockedFiles)
        {
            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }

        private void VisualComposition_Error(object? sender, [NotNull] TextEventArgs e)
        {
            _tracer.TraceError(e.Text);
        }

        private const string Subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        private static int FrameworkVersion()
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ndpKey = baseKey.OpenSubKey(Subkey);
            return (int?)ndpKey?.GetValue("Release") ?? 0;
        }
    }
}
