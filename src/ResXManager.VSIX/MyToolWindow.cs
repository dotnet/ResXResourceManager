namespace ResXManager.VSIX;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Community.VisualStudio.Toolkit;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.VSIX.Compatibility;
using ResXManager.VSIX.Visuals;

using TomsToolbox.Composition;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition.XamlExtensions;

using MessageBoxResult = Microsoft.VisualStudio.VSConstants.MessageBoxResult;

using static Microsoft.VisualStudio.Shell.ThreadHelper;
using ResXManager.VSIX.Properties;

/// <summary>
/// This class implements the tool window exposed by this package and hosts a user control.
/// </summary>
[Guid("79664857-03bf-4bca-aa54-ec998b3328f8")]
public sealed class MyToolWindow : ToolWindowPane
{
    private readonly ITracer _tracer;
    private readonly IConfiguration _configuration;
    private readonly IExportProvider _exportProvider;
    private readonly IVsixCompatibility _vsixCompatibility;
    private readonly ResourceManager _resourceManager;

    private readonly ContentControl _contentWrapper = new()
    {
        Focusable = false,
        Content = new Border { Background = Brushes.Red }
    };

    /// <summary>
    /// Standard constructor for the tool window.
    /// </summary>
    public MyToolWindow()
        : base(null)
    {
        // Set the window title reading it from the resources.
        Caption = Model.Properties.Resources.Title;

        // Set the image that will appear on the tab of the window frame when docked with an other window.
        // The resource ID correspond to the one defined in the resx file while the Index is the offset in the bitmap strip.
        // Each image in the strip being 16x16.
        BitmapResourceID = 301;
        BitmapIndex = 0;

        var exportProvider = VsPackage.Instance.ExportProvider;
        _tracer = exportProvider.GetExportedValue<ITracer>();
        _configuration = exportProvider.GetExportedValue<IConfiguration>();
        _vsixCompatibility = exportProvider.GetExportedValue<IVsixCompatibility>();

        _resourceManager = exportProvider.GetExportedValue<ResourceManager>();
        _resourceManager.BeginEditing += ResourceManager_BeginEditing;
        _resourceManager.Reloading += ResourceManager_Reloading;

        _exportProvider = exportProvider;

        VisualComposition.Error += VisualComposition_Error;
        _contentWrapper.Loaded += ContentWrapper_Loaded;
        _contentWrapper.Unloaded += ContentWrapper_Unloaded;
    }

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
            VS.MessageBox.ShowError(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
        }
    }

    private void ContentWrapper_Loaded(object? sender, RoutedEventArgs e)
    {
        ThrowIfNotOnUIThread();

        try
        {
            VsPackage.Instance.ToolWindowLoaded();

            var view = _exportProvider.GetExportedValue<VsixShellView>();

            _contentWrapper.Content = view;

            _vsixCompatibility.SetFontSize(view);
        }
        catch (Exception ex)
        {
            _tracer.TraceError("ContentWrapper_Loaded failed: " + ex);
            VS.MessageBox.ShowError(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionLoadingError, ex.Message));
        }
    }

    private void ContentWrapper_Unloaded(object? sender, RoutedEventArgs e)
    {
        VsPackage.Instance.ToolWindowUnloaded();
        _contentWrapper.Content = null;
    }

    private static void Navigate_Click(object? sender, RoutedEventArgs e)
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
    private static void CreateWebBrowser(string url)
    {
        Process.Start(url);
    }

    private void ResourceManager_BeginEditing(object? sender, ResourceBeginEditingEventArgs e)
    {
        ThrowIfNotOnUIThread();

        if (!CanEdit(e.Entity, e.CultureKey))
        {
            e.Cancel = true;
        }
    }

    private void ResourceManager_Reloading(object? sender, CancelEventArgs e)
    {
        if (!_resourceManager.HasChanges)
            return;

        if (MessageBoxResult.IDYES == VS.MessageBox.Show(View.Properties.Resources.Title, View.Properties.Resources.WarningUnsavedChanges, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND))
            return;

        e.Cancel = true;
    }

    private bool CanEdit(ResourceEntity entity, CultureKey? cultureKey)
    {
        ThrowIfNotOnUIThread();

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
                VS.MessageBox.ShowError(Model.Properties.Resources.Title, string.Format(CultureInfo.CurrentCulture, View.Properties.Resources.ErrorAddingNewResourceFile, ex));
            }
        }

        if (_vsixCompatibility.ActivateAlreadyOpenEditor(languages))
            return false;

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

        var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasReadOnlyFiles, FormatFileNames(lockedFiles));
        VS.MessageBox.Show(Model.Properties.Resources.Title, message);
        return false;
    }

    private bool QueryEditFiles(string[] lockedFiles)
    {
        ThrowIfNotOnUIThread();

        if (GetService(typeof(SVsQueryEditQuerySave)) is IVsQueryEditQuerySave2 service)
        {
            if ((0 != service.QueryEditFiles(0, lockedFiles.Length, lockedFiles, null, null, out var editVerdict, out _))
                || (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] GetLockedFiles(IEnumerable<ResourceLanguage> languages)
    {
        return languages.Where(l => !l.ProjectFile.IsWritable)
            .Select(l => l.FileName)
            .ToArray();
    }

    private bool AddLanguage(ResourceEntity entity, CultureInfo culture)
    {
        ThrowIfNotOnUIThread();

        var resourceLanguages = entity.Languages;
        if (!resourceLanguages.Any())
            return false;

        if (_configuration.ConfirmAddLanguageFile)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Resources.ProjectHasNoResourceFile, culture.DisplayName);

            if (VS.MessageBox.Show(Model.Properties.Resources.Title, message, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_YESNO) != MessageBoxResult.IDYES)
                return false;
        }

        var neutralLanguage = resourceLanguages.First();
        var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(culture);

        if (!File.Exists(languageFileName))
        {
            var directoryName = Path.GetDirectoryName(languageFileName);
            if (!directoryName.IsNullOrEmpty())
                Directory.CreateDirectory(directoryName);

            File.WriteAllText(languageFileName, Model.Properties.Resources.EmptyResxTemplate);
        }

        _vsixCompatibility.AddProjectItems(entity, neutralLanguage, languageFileName);

        return true;
    }

    [Localizable(false)]
    private static string FormatFileNames(IEnumerable<string> fileNames)
    {
        return string.Join("\n", fileNames.Select(x => "\xA0-\xA0" + x));
    }

    private void VisualComposition_Error(object? sender, TextEventArgs e)
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

    protected override bool PreProcessMessage(ref System.Windows.Forms.Message m)
    {
        // some keys may not be passed through VS
        if (!TryGetKeyForPrivateProcessing(ref m, out var key))
        {
            return base.PreProcessMessage(ref m);
        }

        var keyboardDevice = Keyboard.PrimaryDevice;

        var e = new KeyEventArgs(keyboardDevice, keyboardDevice.ActiveSource, 0, key)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        };

        InputManager.Current.ProcessInput(e);

        return true;
    }

    private static bool TryGetKeyForPrivateProcessing(ref System.Windows.Forms.Message m, out Key key)
    {
        key = default;

        if (m.Msg != 0x0100)
            return false;

        if (m.WParam == (IntPtr)27)
        {
            // https://github.com/dotnet/ResXResourceManager/issues/397
            // must process ESC key here, else window will loose focus without notification.
            key = Key.Escape;
            return true;
        }

        if ((m.WParam == (IntPtr)0x46 || m.WParam == (IntPtr)0x66) && (Keyboard.Modifiers == ModifierKeys.Control))
        {
            // process Ctrl+F locally, we do our own search
            key = Key.F;
            return true;
        }

        return false;
    }
}