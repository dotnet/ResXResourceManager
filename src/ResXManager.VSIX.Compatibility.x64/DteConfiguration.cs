namespace ResXManager.VSIX;

using System;
using System.ComponentModel;
using System.Composition;

using Microsoft.VisualStudio.Shell;

using ResXManager.Infrastructure;
using ResXManager.Model;
using ResXManager.VSIX.Compatibility;

using TomsToolbox.Essentials;

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

        var solutionKey = GetSolutionKey(key);

        if (!TryGetValueFromSolutionGlobals<T>(solutionKey, out var value)) 
            return base.InternalGetValue(defaultValue, key);

        Tracer.WriteLine("Convert old solution settings to new file based settings for key {0}, value {1}", solutionKey, value);

        // Convert old solution settings to new ones.
        TryClearValueFromSolutionGlobals(solutionKey);
        
        base.InternalSetValue(value, key, false);
        
        return value;
    }

    protected override void InternalSetValue<T>(T? value, string key, bool forceGlobal) where T : default
    {
        ThrowIfNotOnUIThread();

        TryClearValueFromSolutionGlobals(GetSolutionKey(key));
        
        base.InternalSetValue(value, key, forceGlobal);
    }

    private bool TryGetValueFromSolutionGlobals<T>(string key, out T? value)
    {
        ThrowIfNotOnUIThread();

        value = default;

        try
        {
            var globals = _solution.Globals;

            if ((globals == null) || !globals.VariableExists[key])
                return false;

            var rawValue = globals[key] as string;

            if (rawValue.IsNullOrEmpty())
                return false;

            value = ConvertFromString(rawValue, value);

            return true;
        }
        catch (Exception ex)
        {
            // Just return false in case of errors. If there is some garbage in the solution, fallback to the default.
            Tracer.WriteLine("Error reading configuration value for {0} from solution file: {1}", key, ex.Message);
            return false;
        }
    }

    private void TryClearValueFromSolutionGlobals(string key)
    {
        ThrowIfNotOnUIThread();

        try
        {
            var globals = _solution.Globals;
            if (globals == null)
                return;

            if (!globals.VariableExists[key])
                return;

            globals.VariablePersists[key] = false;
            globals[key] = null;
        }
        catch (Exception ex)
        {
            Tracer.TraceError("Error clearing configuration value for {0} in solution file: {1}", key, ex.Message);
        }
    }

    private static string GetSolutionKey(string? propertyName)
    {
        return "RESX_" + propertyName;
    }
}
