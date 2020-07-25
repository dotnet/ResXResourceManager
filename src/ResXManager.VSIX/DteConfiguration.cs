namespace ResXManager.VSIX
{
    using System;
    using System.ComponentModel;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    [Export(typeof(DteConfiguration))]
    internal class DteConfiguration : Configuration
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
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                return _solution.Globals != null ? ConfigurationScope.Solution : ConfigurationScope.Global;
            }
        }

        [return: MaybeNull]
        protected override T InternalGetValue<T>([AllowNull] T defaultValue, string key)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return TryGetValue(GetKey(key), defaultValue, out var value) ? value : base.InternalGetValue(defaultValue, key);
        }

        protected override void InternalSetValue<T>([AllowNull] T value, string key)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var globals = _solution.Globals;

            if (globals != null)
            {
                TrySetValue(globals, GetKey(key), value);
            }
            else
            {
                base.InternalSetValue(value, key);
            }
        }

        private bool TryGetValue<T>(string? key, [AllowNull] T defaultValue, [MaybeNull] out T value)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            value = defaultValue;

            return TryGetValue(_solution.Globals, key, ref value);
        }

        private static bool TryGetValue<T>(EnvDTE.Globals? globals, string? key, [AllowNull] ref T value)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

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

        private void TrySetValue<T>(EnvDTE.Globals globals, string? internalKey, [AllowNull] T value)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

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
