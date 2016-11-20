namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Visuals;
    using tomenglertde.ResXManager.VSIX.Visuals;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Desktop.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.XamlExtensions;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("79664857-03bf-4bca-aa54-ec998b3328f8")]
    public sealed class MyToolWindow : ToolWindowPane, IVsServiceProvider, ISourceFilesProvider
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost = new CompositionHost();

        [NotNull]
        private readonly ITracer _trace;
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        private EnvDTE.DTE _dte;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow()
            : base(null)
        {
            try
            {
                // Set the window title reading it from the resources.
                Caption = Resources.ToolWindowTitle;

                // Set the image that will appear on the tab of the window frame when docked with an other window.
                // The resource ID correspond to the one defined in the resx file while the Index is the offset in the bitmap strip.
                // Each image in the strip being 16x16.
                BitmapResourceID = 301;
                BitmapIndex = 1;

                var path = Path.GetDirectoryName(GetType().Assembly.Location);
                Contract.Assume(path != null);

                _compositionHost.AddCatalog(new DirectoryCatalog(path, @"*.dll"));
                _compositionHost.ComposeExportedValue((IVsServiceProvider)this);
                _compositionHost.ComposeExportedValue((ISourceFilesProvider)this);

                _trace = _compositionHost.GetExportedValue<ITracer>();
                _performanceTracer = _compositionHost.GetExportedValue<PerformanceTracer>();
                _configuration = _compositionHost.GetExportedValue<Configuration>();

                _resourceManager = _compositionHost.GetExportedValue<ResourceManager>();
                _resourceManager.BeginEditing += ResourceManager_BeginEditing;

                _resourceViewModel = _compositionHost.GetExportedValue<ResourceViewModel>();

                VisualComposition.Error += VisualComposition_Error;
            }
            catch (Exception ex)
            {
                _trace.TraceError("MyToolWindow .ctor failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
        }

        [NotNull]
        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);

                return _resourceManager;
            }
        }

        [NotNull]
        public ResourceViewModel ResourceViewModel
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceViewModel>() != null);

                return _resourceViewModel;
            }
        }

        [NotNull]
        public ICompositionHost CompositionHost
        {
            get
            {
                Contract.Ensures(Contract.Result<ICompositionHost>() != null);

                return _compositionHost;
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                _trace.WriteLine(Resources.IntroMessage);

                var view = _compositionHost.GetExportedValue<VsixShellView>();

                view.DataContext = _compositionHost.GetExportedValue<VsixShellViewModel>();
                view.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));
                view.Loaded += view_Loaded;
                view.IsKeyboardFocusWithinChanged += view_IsKeyboardFocusWithinChanged;
                view.Track(UIElement.IsMouseOverProperty).Changed += view_IsMouseOverChanged;

                _dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                Contract.Assume(_dte != null);

                var executingAssembly = Assembly.GetExecutingAssembly();
                var folder = Path.GetDirectoryName(executingAssembly.Location);

                _trace.WriteLine(Resources.AssemblyLocation, folder);
                _trace.WriteLine(Resources.Version, new AssemblyName(executingAssembly.FullName).Version);

                EventManager.RegisterClassHandler(typeof(VsixShellView), ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));

                // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
                // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
                // the object returned by the Content property.
                Content = view;

                _dte.SetFontSize(view);
            }
            catch (Exception ex)
            {
                _trace.TraceError("MyToolWindow OnCreate failed: " + ex);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
        }

        protected override void OnClose()
        {
            base.OnClose();

            _compositionHost.Dispose();
        }

        private void view_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadSolution();
        }

        private void Navigate_Click(object sender, [NotNull] RoutedEventArgs e)
        {
            string url;

            var source = e.OriginalSource as FrameworkElement;
            if (source != null)
            {
                var button = source.TryFindAncestorOrSelf<ButtonBase>();
                if (button == null)
                    return;

                url = source.Tag as string;
                if (string.IsNullOrEmpty(url) || !url.StartsWith(@"http", StringComparison.OrdinalIgnoreCase))
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

        private void view_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!true.Equals(e.NewValue))
                return;

            ReloadSolution();
        }

        private void view_IsMouseOverChanged(object sender, EventArgs e)
        {
            var view = sender as UIElement;

            if ((view == null) || !view.IsMouseOver)
                return;

            ReloadSolution();
        }

        [Localizable(false)]
        private void CreateWebBrowser([NotNull] string url)
        {
            Contract.Requires(url != null);

            var webBrowsingService = (IVsWebBrowsingService)GetService(typeof(SVsWebBrowsingService));
            if (webBrowsingService != null)
            {
                IVsWindowFrame pFrame;
                var hr = webBrowsingService.Navigate(url, (uint)__VSWBNAVIGATEFLAGS.VSNWB_WebURLOnly, out pFrame);
                if (ErrorHandler.Succeeded(hr) && (pFrame != null))
                {
                    hr = pFrame.Show();
                    if (ErrorHandler.Succeeded(hr))
                        return;
                }
            }

            Process.Start(url);
        }

        private void ResourceManager_BeginEditing(object sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, CultureKey cultureKey)
        {
            Contract.Requires(entity != null);

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

                ActivateWindow(alreadyOpenItems.First()?.Item2);

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

        private static void ActivateWindow(EnvDTE.Window window)
        {
            try
            {
                window?.Activate();
            }
            catch { }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [NotNull]
        private Tuple<string, EnvDTE.Window>[] GetLanguagesOpenedInAnotherEditor([NotNull] IEnumerable<ResourceLanguage> languages)
        {
            Contract.Requires(languages != null);
            Contract.Ensures(Contract.Result<Tuple<string, EnvDTE.Window>[]>() != null);

            try
            {
                var openDocuments = _dte?.Windows?.OfType<EnvDTE.Window>().ToDictionary(window => window.Document);

                var items = from l in languages
                            let file = l.FileName
                            let projectFile = l.ProjectFile as DteProjectFile
                            let documents = projectFile?.ProjectItems.Select(item => item.TryGetDocument()).Where(doc => doc != null)
                            let window = documents?.Select(doc => openDocuments?.GetValueOrDefault(doc)).FirstOrDefault(win => win != null)
                            where window != null
                            select Tuple.Create(file, window);

                return items.ToArray();
            }
            catch
            {
                return new Tuple<string, EnvDTE.Window>[0];
            }
        }

        private bool QueryEditFiles([NotNull] string[] lockedFiles)
        {
            Contract.Requires(lockedFiles != null);
            var service = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));
            if (service != null)
            {
                uint editVerdict;
                uint moreInfo;

                if ((0 != service.QueryEditFiles(0, lockedFiles.Length, lockedFiles, null, null, out editVerdict, out moreInfo))
                    || (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    return false;
                }
            }
            return true;
        }

        [NotNull]
        private static string[] GetLockedFiles([NotNull] IEnumerable<ResourceLanguage> languages)
        {
            Contract.Requires(languages != null);

            return languages.Where(l => !l.ProjectFile.IsWritable)
                .Select(l => l.FileName)
                .ToArray();
        }

        private bool AddLanguage([NotNull] ResourceEntity entity, [NotNull] CultureInfo culture)
        {
            Contract.Requires(entity != null);
            Contract.Requires(culture != null);

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
            Contract.Assume(neutralLanguage != null);

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
            Contract.Requires(entity != null);
            Contract.Requires(neutralLanguage != null);
            Contract.Requires(!string.IsNullOrEmpty(languageFileName));

            DteProjectFile projectFile = null;

            foreach (var neutralLanguageProjectItem in ((DteProjectFile)neutralLanguage.ProjectFile).ProjectItems)
            {
                Contract.Assume(neutralLanguageProjectItem != null);

                var collection = neutralLanguageProjectItem.Collection;
                Contract.Assume(collection != null);

                var projectItem = collection.AddFromFile(languageFileName);
                Contract.Assume(projectItem != null);

                var containingProject = projectItem.ContainingProject;
                Contract.Assume(containingProject != null);

                var projectName = containingProject.Name;
                Contract.Assume(projectName != null);

                if (projectFile == null)
                {
                    projectFile = new DteProjectFile(_compositionHost.GetExportedValue<DteSolution>(), languageFileName, projectName, containingProject.UniqueName, projectItem);
                }
                else
                {
                    projectFile.AddProject(projectName, projectItem);
                }
            }

            if (projectFile != null)
            {
                entity.AddLanguage(projectFile, _configuration.DuplicateKeyHandling);
            }
        }

        [NotNull]
        [Localizable(false)]
        private static string FormatFileNames([NotNull] IEnumerable<string> lockedFiles)
        {
            Contract.Requires(lockedFiles != null);
            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                using (_performanceTracer.Start("Enumerate source files"))
                {
                    return DteSourceFiles.Cast<ProjectFile>().ToArray();
                }
            }
        }

        [NotNull]
        private IEnumerable<DteProjectFile> DteSourceFiles
        {
            get
            {
                var sourceFileFilter = new SourceFileFilter(_configuration);

                return GetProjectFiles().Where(p => p.IsResourceFile() || sourceFileFilter.IsSourceFile(p));
            }
        }

        internal void ReloadSolution()
        {
            try
            {
                using (_performanceTracer.Start("Reload solution"))
                {
                    _resourceViewModel.Reload();
                }
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
            }
        }

        [NotNull]
        private IEnumerable<DteProjectFile> GetProjectFiles()
        {
            return _compositionHost.GetExportedValue<DteSolution>().GetProjectFiles();
        }

        private void VisualComposition_Error(object sender, [NotNull] TextEventArgs e)
        {
            _trace.TraceError(e.Text);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_trace != null);
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_resourceViewModel != null);
        }
    }
}
