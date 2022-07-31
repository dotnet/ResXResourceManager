namespace ResXManager.VSIX
{
    using System;
    using System.ComponentModel;
    using System.Composition;

    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.VSIX.Compatibility;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Shared]
    [Export(typeof(IConfiguration))]
    [Export(typeof(IDteConfiguration))]
    internal class DteConfiguration : Configuration, IDteConfiguration
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

        public override bool IsScopeSupported => true;

        public override ConfigurationScope Scope
        {
            get
            {
                ThrowIfNotOnUIThread();
                return _solution.Globals != null ? ConfigurationScope.Solution : ConfigurationScope.Global;
            }
        }

        protected override T? InternalGetValue<T>(T? defaultValue, string key) where T : default
        {
            ThrowIfNotOnUIThread();

            return TryGetValue(GetKey(key), defaultValue, out var value) ? value : base.InternalGetValue(defaultValue, key);
        }

        protected override void InternalSetValue<T>(T? value, string key, bool forceGlobal) where T : default
        {
            ThrowIfNotOnUIThread();

            var globals = _solution.Globals;

            if (globals != null && !forceGlobal)
            {
                TrySetValue(globals, GetKey(key), value);
            }
            else
            {
                base.InternalSetValue(value, key, forceGlobal);
            }
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

        private void TrySetValue<T>(EnvDTE.Globals globals, string? internalKey, T? value)
        {
            ThrowIfNotOnUIThread();

            try
            {
                globals[internalKey] = ConvertToString(value);
                globals.VariablePersists[internalKey] = true;
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
}
