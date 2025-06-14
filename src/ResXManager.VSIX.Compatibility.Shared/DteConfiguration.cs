namespace ResXManager.VSIX;

using System;
using System.ComponentModel;
using System.Composition;

using Microsoft.VisualStudio.Shell;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.VSIX.Compatibility;

using static Microsoft.VisualStudio.Shell.ThreadHelper;

using Configuration = ResXManager.Model.Configuration;

[Shared]
[Export(typeof(IConfiguration))]
[Export(typeof(IDteConfiguration))]
internal sealed class DteConfiguration : Configuration, IDteConfiguration
{
    private readonly DteSolution _solution;

    [ImportingConstructor]
    // ReSharper disable once NotNullMemberIsNotInitialized
#pragma warning disable 8618
    public DteConfiguration(DteSolution solution, ITracer tracer)
#pragma warning restore 8618
        : base(tracer)
    {
        _solution = solution;
    }

    [DefaultValue(MoveToResourceConfiguration.Default)]
    public MoveToResourceConfiguration MoveToResources { get; }

    [DefaultValue(true)]
    public bool ShowErrorsInErrorList { get; set; }

    [DefaultValue(TaskErrorCategory.Warning)]
    public TaskErrorCategory TaskErrorCategory { get; set; }

    protected override T? InternalGetValue<T>(T? defaultValue, string key) where T : default
    {
        ThrowIfNotOnUIThread();

        // Convert old solution settings to new ones.
        if (!TryGetValue(GetKey(key), defaultValue, out var value)) 
            return base.InternalGetValue(defaultValue, key);

        TryClearValue<T>(_solution.Globals, GetKey(key));
        base.InternalSetValue(value, key, false);
        return value;
    }

    protected override void InternalSetValue<T>(T? value, string key, bool forceGlobal) where T : default
    {
        ThrowIfNotOnUIThread();

        TryClearValue<T>(_solution.Globals, GetKey(key));
        
        base.InternalSetValue(value, key, forceGlobal);
    }

    private bool TryGetValue<T>(string? key, T? defaultValue, out T? value)
    {
        ThrowIfNotOnUIThread();

        value = defaultValue;

        return TryGetValue(_solution.Globals, key, ref value);
    }

    private static bool TryGetValue<T>(EnvDTE.Globals? globals, string? key, ref T? value)
    {
        ThrowIfNotOnUIThread();

        try
        {
            if ((globals != null) && globals.VariableExists[key])
            {
                value = ConvertFromString(globals[key] as string, value);
                return true;
            }
        }
        catch
        {
            // Just return false in case of errors. If there is some garbage in the solution, fallback to the default.
        }

        return false;
    }

    private void TryClearValue<T>(EnvDTE.Globals? globals, string? internalKey)
    {
        ThrowIfNotOnUIThread();

        if (globals == null || string.IsNullOrEmpty(internalKey))
            return;

        try
        {
            globals.VariablePersists[internalKey] = false;
        }
        catch (Exception ex)
        {
            Tracer.TraceError("Error saving configuration value to solution: {0}", ex.Message);
        }
    }

    private static string GetKey(string? propertyName)
    {
        return @"RESX_" + propertyName;
    }
}
