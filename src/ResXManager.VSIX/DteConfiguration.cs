namespace ResXManager.VSIX
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    [Export(typeof(DteConfiguration))]
    internal class DteConfiguration : Configuration
    {
        [NotNull]
        private readonly DteSolution _solution;

        [ImportingConstructor]
        // ReSharper disable once NotNullMemberIsNotInitialized
#pragma warning disable 8618
        public DteConfiguration([NotNull] DteSolution solution, [NotNull] ITracer tracer)
#pragma warning restore 8618
            : base(tracer)
        {
            _solution = solution;
        }

        [NotNull, UsedImplicitly]
        [DefaultValue(MoveToResourceConfiguration.Default)]
        public MoveToResourceConfiguration MoveToResources { get; }

        [UsedImplicitly]
        [DefaultValue(true)]
        public bool ShowErrorsInErrorList { get; set; }

        [UsedImplicitly]
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

        [CanBeNull]
        protected override T InternalGetValue<T>([CanBeNull] T defaultValue, string key)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return TryGetValue(GetKey(key), defaultValue, out var value) ? value : base.InternalGetValue(defaultValue, key);
        }

        protected override void InternalSetValue<T>([CanBeNull] T value, string key)
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

        private bool TryGetValue<T>(string? key, [CanBeNull] T defaultValue, [CanBeNull] out T value)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            value = defaultValue;

            return TryGetValue(_solution.Globals, key, ref value);
        }

        private static bool TryGetValue<T>(EnvDTE.Globals? globals, string? key, [CanBeNull] ref T value)
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

        private void TrySetValue<T>([NotNull] EnvDTE.Globals globals, string? internalKey, [CanBeNull] T value)
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

        [NotNull]
        private static string GetKey(string? propertyName)
        {
            return @"RESX_" + propertyName;
        }
    }
}
