namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Visuals;
    using VSLangProj;
    using Process = System.Diagnostics.Process;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("79664857-03bf-4bca-aa54-ec998b3328f8")]
    public sealed class MyToolWindow : ToolWindowPane
    {
        private const string CATEGORY_FONTS_AND_COLORS = "FontsAndColors";
        private const string PAGE_TEXT_EDITOR = "TextEditor";
        private const string PROPERTY_FONT_SIZE = "FontSize";

        private DTE _dte;
        private ResourceView _view;
        private ResourceManager _resourceManager;
        private string _solutionFingerPrint;
        private readonly OutputWindowTracer _trace;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow()
            : base(null)
        {
            Contract.Ensures(BitmapIndex == 1);

            // Set the window title reading it from the resources.
            Caption = Resources.ToolWindowTitle;

            // Set the image that will appear on the tab of the window frame when docked with an other window.
            // The resource ID correspond to the one defined in the resx file while the Index is the offset in the bitmap strip.
            // Each image in the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;

            _trace = new OutputWindowTracer(this);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                _trace.WriteLine(Resources.IntroMessage);

                var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (folder != null)
                    _trace.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.AssemblyLocation, folder));

                AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;

                _resourceManager = new ResourceManager();
                _view = new ResourceView { DataContext = _resourceManager };
                _view.Loaded += view_Loaded;
                _view.NavigateClick += view_NavigateClick;
                _view.BeginEditing += view_BeginEditing;
                _view.ReloadRequested += view_ReloadRequested;
                _view.LanguageSaved += view_LanguageSaved;
                _view.IsKeyboardFocusWithinChanged += view_IsKeyboardFocusWithinChanged;

                // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
                // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
                // the object returned by the Content property.
                Content = _view;

                _dte = (DTE)GetService(typeof(DTE));
                if (_dte == null)
                    return;

                try
                {
                    var properties = _dte.Properties[CATEGORY_FONTS_AND_COLORS, PAGE_TEXT_EDITOR];
                    var fontSize = Convert.ToDouble(properties.Item(PROPERTY_FONT_SIZE).Value);
                    // Default in VS is 10, but looks like 12 in WPF
                    _view.TextFontSize = fontSize * 1.2;
                }
                catch { }

                ReloadSolution();

                AppDomain.CurrentDomain.AssemblyResolve -= AppDomain_AssemblyResolve;
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
            }
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

        private void view_NavigateClick(object sender, RoutedEventArgs e)
        {
            var source = e.Source as FrameworkElement;
            if (source == null)
                return;

            var url = source.Tag as string;
            if (string.IsNullOrEmpty(url))
                return;

            CreateWebBrowser(url);
        }

        void view_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
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

        [Localizable(false)]
        private void CreateWebBrowser(string url)
        {
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

        private void view_BeginEditing(object sender, ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.Language))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit(ResourceEntity entity, CultureInfo language)
        {
            Contract.Requires(entity != null);

            var languages = entity.Languages.Where(lang => (language == null) || language.Equals(lang.Culture)).ToArray();

            if (!languages.Any())
            {
                try
                {
                    // because entity.Languages.Any() => languages can only be empty if language != null!
                    Contract.Assume(language != null);

                    return AddLanguage(entity, language);
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
            var lockedFiles = languages.Where(l => !l.IsWritable).Select(l => l.FileName).ToArray();

            if (!lockedFiles.Any())
                return true;

            var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));

            MessageBox.Show(message, Resources.ToolWindowTitle);
            return false;
        }

        private bool AddLanguage(ResourceEntity entity, CultureInfo language)
        {
            Contract.Requires(entity != null);
            Contract.Requires(language != null);

            var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasNoResourceFile, language.DisplayName);

            if (MessageBox.Show(message, Resources.ToolWindowTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return false;

            var neutralLanguage = entity.Languages.First();

            var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(language);

            if (File.Exists(languageFileName))
            {
                if (MessageBox.Show(string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.FileExistsPrompt, languageFileName), Resources.ToolWindowTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(languageFileName));

            File.WriteAllText(languageFileName, View.Properties.Resources.EmptyResxTemplate);

            AddProjectItems(entity, neutralLanguage, languageFileName);

            return true;
        }

        private void AddProjectItems(ResourceEntity entity, ResourceLanguage neutralLanguage, string languageFileName)
        {
            Contract.Requires(entity != null);
            Contract.Requires(neutralLanguage != null);
            Contract.Requires(languageFileName != null);

            DteProjectFile projectFile = null;

            foreach (var neutralLanguageProjectItem in ((DteProjectFile)neutralLanguage.ProjectFile).ProjectItems.OfType<ProjectItem>())
            {
                var collection = neutralLanguageProjectItem.Collection;
                Contract.Assume(collection != null);

                var projectItem = collection.AddFromFile(languageFileName);
                Contract.Assume(projectItem != null);

                var containingProject = projectItem.ContainingProject;

                if (projectFile == null)
                {
                    var solutionFolder = Path.GetDirectoryName(_dte.Solution.FullName);
                    projectFile = new DteProjectFile(languageFileName, solutionFolder, containingProject.Name, containingProject.UniqueName, projectItem);
                }
                else
                {
                    projectFile.AddProject(containingProject.Name, projectItem);
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

        private void view_ReloadRequested(object sender, EventArgs e)
        {
            Solution_Changed(true);
        }

        private void view_LanguageSaved(object sender, LanguageEventArgs e)
        {
            // WE have saved the files - update the finger print so we don't reload unnecessarily
            _solutionFingerPrint = GetFingerprint(GetProjectFiles());

            var projectItems = ((DteProjectFile)e.Language.ProjectFile).ProjectItems
                .Where(projectItem => projectItem != null);

            foreach (var projectItem in projectItems)
            {
                if (projectItem.IsOpen && (projectItem.Document != null))
                {
                    projectItem.Document.Close();
                }

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
            if (_view == null)
                return;

            var projectFiles = GetProjectFiles().Where(p => p.IsResourceFile() || p.IsSourceCodeOrContentFile()).Cast<ProjectFile>().ToArray();

            // The solution events are not reliable, so we check the solution on every load/unload of our window.
            // To avoid loosing the scope every time this method is called we only call load if we detect changes.
            var fingerPrint = GetFingerprint(projectFiles);

            if (!forceReload && fingerPrint.Equals(_solutionFingerPrint, StringComparison.OrdinalIgnoreCase))
                return;

            _solutionFingerPrint = fingerPrint;
            _resourceManager.Load(projectFiles);

            if (View.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                CodeReference.BeginFind(_resourceManager.ResourceEntities, projectFiles, _trace);
            }
        }

        private IEnumerable<DteProjectFile> GetProjectFiles()
        {
            if ((_dte == null) || (_view == null))
                return Enumerable.Empty<DteProjectFile>();

            var solution = _dte.Solution;
            if ((solution == null) || (solution.Projects == null))
                return Enumerable.Empty<DteProjectFile>();

            return solution.GetProjectFiles(_trace);
        }

        private static string GetFingerprint(IEnumerable<ProjectFile> allFiles)
        {
            Contract.Requires(allFiles != null);

            var fileKeys = allFiles
                .Where(file => file.IsResourceFile())
                .Select(file => file.FilePath)
                .Select(filePath => filePath + @":" + File.GetLastWriteTime(filePath).ToString(CultureInfo.InvariantCulture))
                .OrderBy(fileKey => fileKey);

            return string.Join(@"|", fileKeys);
        }

        private Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (folder == null)
                return null;
            if (args.Name == null)
                return null;

            var assemblySimpleName = new AssemblyName(args.Name).Name;

            if (assemblySimpleName.EndsWith(@".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var filePath = Path.Combine(folder, assemblySimpleName + ".dll");

            _trace.WriteLine(string.Format(CultureInfo.CurrentCulture, @"Resolve assembly {0} => {1}", args.Name, filePath));

            if (!File.Exists(filePath))
            {
                _trace.TraceError(string.Format(CultureInfo.CurrentCulture, @"File not found: {0}", filePath));
                return null;
            }

            try
            {
                var assembly = Assembly.Load(AssemblyName.GetAssemblyName(filePath));
                return assembly;
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.ToString());
                throw;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_trace != null);
        }
    }
}
