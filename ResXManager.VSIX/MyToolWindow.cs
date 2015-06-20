namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;

    using EnvDTE;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    using VSLangProj;

    using Process = System.Diagnostics.Process;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("79664857-03bf-4bca-aa54-ec998b3328f8")]
    public sealed class MyToolWindow : ToolWindowPane, IVsServiceProvider
    {
        private readonly ICompositionHost _compositionHost = new CompositionHost();

        private readonly ITracer _trace;
        private readonly ResourceManager _resourceManager;
        private readonly Control _view;

        private DTE _dte;
        private string _solutionFingerPrint;
        private string _currentSolutionFullName;

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

            _compositionHost.AddCatalog(new DirectoryCatalog(Path.GetDirectoryName(GetType().Assembly.Location), "ResXManager.*.dll"));
            _compositionHost.ComposeExportedValue((IVsServiceProvider)this);

            ExportProviderLocator.Register(_compositionHost.Container);

            _trace = _compositionHost.GetExportedValue<ITracer>();

            _resourceManager = _compositionHost.GetExportedValue<ResourceManager>();
            _resourceManager.BeginEditing += ResourceManager_BeginEditing;
            _resourceManager.ReloadRequested += ResourceManager_ReloadRequested;
            _resourceManager.LanguageSaved += ResourceManager_LanguageSaved;

            _view = new Shell { DataContext = _resourceManager };
            _view.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(_compositionHost.Container));
            _view.Loaded += view_Loaded;
            _view.IsKeyboardFocusWithinChanged += view_IsKeyboardFocusWithinChanged;
            _view.Track(UIElement.IsMouseOverProperty).Changed += view_IsMouseOverChanged;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                _trace.WriteLine(Resources.IntroMessage);

                _dte = (DTE)GetService(typeof(DTE));
                Contract.Assume(_dte != null);

                var executingAssembly = Assembly.GetExecutingAssembly();
                var folder = Path.GetDirectoryName(executingAssembly.Location);

                _trace.WriteLine(Resources.AssemblyLocation, folder);
                _trace.WriteLine(Resources.Version, new AssemblyName(executingAssembly.FullName).Version);

                EventManager.RegisterClassHandler(typeof(Shell), ButtonBase.ClickEvent, new RoutedEventHandler(Navigate_Click));

                // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
                // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
                // the object returned by the Content property.
                Content = _view;

                _dte.SetFontSize(_view);

                ReloadSolution();
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
        }

        protected override void OnClose()
        {
            base.OnClose();

            _compositionHost.Dispose();
        }

        /* Maybe use that later...
        private void FindSymbol()
        {
            var findSymbol = (IVsFindSymbol)GetService(typeof(SVsObjectSearch));

            var vsSymbolScopeAll = new Guid(0xa5a527ea, 0xcf0a, 0x4abf, 0xb5, 0x1, 0xea, 0xfe, 0x6b, 0x3b, 0xa5, 0xc6);
            var vsSymbolScopeSolution = new Guid(0xb1ba9461, 0xfc54, 0x45b3, 0xa4, 0x84, 0xcb, 0x6d, 0xd0, 0xb9, 0x5c, 0x94);

            var search = new[]
                {
                    new VSOBSEARCHCRITERIA2
                        {
                            dwCustom = 0,
                            eSrchType = VSOBSEARCHTYPE.SO_ENTIREWORD,
                            grfOptions = (int)(_VSOBSEARCHOPTIONS2.VSOBSO_CALLSTO | _VSOBSEARCHOPTIONS2.VSOBSO_CALLSFROM | _VSOBSEARCHOPTIONS2.VSOBSO_LISTREFERENCES),
                            pIVsNavInfo = null,
                            szName = "HistoryList"
                        },
                };

            try
            {
                var result = findSymbol.DoSearch(vsSymbolScopeSolution, search);

                MessageBox.Show(result.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }*/

        private void view_Loaded(object sender, RoutedEventArgs e)
        {
            Solution_Changed();
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
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
                var link = e.OriginalSource as Hyperlink;
                if (link == null)
                    return;

                var navigateUri = link.NavigateUri;
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

            try
            {
                ReloadSolution();
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
            }
        }

        private void view_IsMouseOverChanged(object sender, EventArgs e)
        {
            if (!_view.IsMouseOver)
                return;

            try
            {
                ReloadSolution();
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
            }
        }

        [Localizable(false)]
        private void CreateWebBrowser(string url)
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

            var languages = entity.Languages.Where(lang => (culture == null) || culture.Equals(lang.Culture)).ToArray();

            if (!languages.Any())
            {
                try
                {
                    // because entity.Languages.Any() => languages can only be empty if culture != null!
                    Contract.Assume(culture != null);

                    return AddLanguage(resourceManager, entity, culture);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex), Resources.ToolWindowTitle);
                }
            }

            var service = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));
            if (service != null)
            {
                var files = languages.Select(l => l.FileName).ToArray();

                uint editVerdict;
                uint moreInfo;

                if ((0 != service.QueryEditFiles(0, files.Length, files, null, null, out editVerdict, out moreInfo))
                    || (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    return false;
                }
            }

            // if file is not under source control, we get an OK even if the file is read only!
            var lockedFiles = languages.Where(l => !l.ProjectFile.IsWritable).Select(l => l.FileName).ToArray();

            if (!lockedFiles.Any())
                return true;

            var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));

            MessageBox.Show(message, Resources.ToolWindowTitle);
            return false;
        }

        private bool AddLanguage(ResourceManager resourceManager, ResourceEntity entity, CultureInfo culture)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(entity != null);
            Contract.Requires(culture != null);

            var resourceLanguages = entity.Languages;
            if (!resourceLanguages.Any())
                return false;

            if (resourceManager.Configuration.ConfirmAddLanguageFile)
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

                File.WriteAllText(languageFileName, View.Properties.Resources.EmptyResxTemplate);
            }

            AddProjectItems(entity, neutralLanguage, languageFileName);

            return true;
        }

        private void AddProjectItems(ResourceEntity entity, ResourceLanguage neutralLanguage, string languageFileName)
        {
            Contract.Requires(entity != null);
            Contract.Requires(neutralLanguage != null);
            Contract.Requires(!string.IsNullOrEmpty(languageFileName));

            DteProjectFile projectFile = null;
            if (_dte == null)
                return;
            var solution = _dte.Solution;
            if (solution == null)
                return;

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
                    var solutionFolder = Path.GetDirectoryName(solution.FullName);
                    Contract.Assume(solutionFolder != null);
                    projectFile = new DteProjectFile(languageFileName, solutionFolder, projectName, containingProject.UniqueName, projectItem);
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

            // WE have saved the files - update the finger print so we don't reload unnecessarily
            _solutionFingerPrint = GetFingerprint(GetProjectFiles());
        }

        [Localizable(false)]
        private static string FormatFileNames(IEnumerable<string> lockedFiles)
        {
            Contract.Requires(lockedFiles != null);
            return string.Join("\n", lockedFiles.Select(x => "\xA0-\xA0" + x));
        }

        private void ResourceManager_ReloadRequested(object sender, EventArgs e)
        {
            Solution_Changed(true);
        }

        private void ResourceManager_LanguageSaved(object sender, LanguageEventArgs e)
        {
            // WE have saved the files - update the finger print so we don't reload unnecessarily
            _solutionFingerPrint = GetFingerprint(GetProjectFiles());

            var projectItems = ((DteProjectFile)e.Language.ProjectFile).ProjectItems
                .Where(projectItem => projectItem != null);

            foreach (var projectItem in projectItems.SelectMany(item => item.DescendantsAndSelf()))
            {
                Contract.Assume(projectItem != null);

                var vsProjectItem = projectItem.Object as VSProjectItem;

                if (vsProjectItem != null)
                {
                    vsProjectItem.RunCustomTool();
                }
            }
        }

        private void Solution_Changed(bool forceReload = false)
        {
            try
            {
                ReloadSolution(forceReload);
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ResourceLoadingError, ex.Message));
            }
        }

        private void ReloadSolution(bool forceReload = false)
        {
            Contract.Assume(_dte != null);

            var solution = _dte.Solution;
            var sourceFileFilter = new SourceFileFilter(_compositionHost.GetExportedValue<Model.Configuration>());

            var projectFiles = GetProjectFiles().Where(p => p.IsResourceFile() || sourceFileFilter.IsSourceFile(p)).ToArray();

            // The solution events are not reliable, so we check the solution on every load/unload of our window.
            // To avoid loosing the scope every time this method is called we only call load if we detect changes.
            var fingerPrint = GetFingerprint(projectFiles);

            if (!forceReload
                && !projectFiles.Where(p => p.IsResourceFile()).Any(p => p.HasChanges)
                && fingerPrint.Equals(_solutionFingerPrint, StringComparison.OrdinalIgnoreCase))
                return;

            var solutionFullName = solution.Maybe().Return(s => s.FullName);

            _solutionFingerPrint = fingerPrint;

            _resourceManager.Load(projectFiles);

            if (!string.Equals(solutionFullName, _currentSolutionFullName, StringComparison.OrdinalIgnoreCase))
            {
                _currentSolutionFullName = solutionFullName;
                _resourceManager.AreAllFilesSelected = true;
            }

            if (Settings.Default.IsFindCodeReferencesEnabled)
            {
                CodeReference.BeginFind(_resourceManager, projectFiles, _trace);
            }
        }

        private IEnumerable<DteProjectFile> GetProjectFiles()
        {
            if (_dte == null)
                return Enumerable.Empty<DteProjectFile>();

            var solution = _dte.Solution;
            if ((solution == null) || (solution.Projects == null))
                return Enumerable.Empty<DteProjectFile>();

            return solution.GetProjectFiles(_trace);
        }

        private static string GetFingerprint(IEnumerable<DteProjectFile> allFiles)
        {
            Contract.Requires(allFiles != null);

            var fileKeys = allFiles
                .Where(file => file.IsResourceFile())
                .Select(file => file.FilePath)
                .Select(filePath => filePath + @":" + File.GetLastWriteTime(filePath).ToString(CultureInfo.InvariantCulture))
                .OrderBy(fileKey => fileKey);

            return string.Join(@"|", fileKeys);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_trace != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_view != null);
        }
    }
}
