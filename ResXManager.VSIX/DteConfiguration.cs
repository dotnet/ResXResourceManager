namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;

    [Export(typeof(Configuration))]
    [Export(typeof(DteConfiguration))]
    internal class DteConfiguration : Configuration
    {
        [NotNull]
        private readonly DteSolution _solution;
        private readonly DispatcherThrottle _moveToResourcesChangeThrottle;
        private MoveToResourceConfiguration _moveToResources;

        [ImportingConstructor]
        public DteConfiguration([NotNull] DteSolution solution, [NotNull] ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(solution != null);
            Contract.Requires(tracer != null);

            _solution = solution;
            _moveToResourcesChangeThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, PersistMoveToResources);
        }

        [NotNull]
        public MoveToResourceConfiguration MoveToResources
        {
            get
            {
                Contract.Ensures(Contract.Result<MoveToResourceConfiguration>() != null);

                return _moveToResources ?? LoadMoveToResourceConfiguration(GetValue(default(MoveToResourceConfiguration)));
            }
        }

        protected override void OnReload()
        {
            _moveToResources = null;

            base.OnReload();
        }

        private void PersistMoveToResources()
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            SetValue(MoveToResources, nameof(MoveToResources));
        }

        [NotNull]
        private MoveToResourceConfiguration LoadMoveToResourceConfiguration(MoveToResourceConfiguration current)
        {
            Contract.Ensures(Contract.Result<MoveToResourceConfiguration>() != null);

            _moveToResources = current ?? MoveToResourceConfiguration.Default;
            _moveToResources.ItemPropertyChanged += (_, __) => _moveToResourcesChangeThrottle.Tick();

            return _moveToResources;
        }

        public override bool IsScopeSupported => true;

        public override ConfigurationScope Scope => _solution.Globals != null ? ConfigurationScope.Solution : ConfigurationScope.Global;

        protected override T InternalGetValue<T>(T defaultValue, string key)
        {
            T value;

            return TryGetValue(GetKey(key), out value) ? value : base.InternalGetValue(defaultValue, key);
        }

        protected override void InternalSetValue<T>(T value, string key)
        {
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

        private bool TryGetValue<T>(string key, out T value)
        {
            value = default(T);

            return TryGetValue(_solution.Globals, key, ref value);
        }

        private static bool TryGetValue<T>(EnvDTE.Globals globals, string key, ref T value)
        {
            try
            {
                if ((globals != null) && globals.VariableExists[key])
                {
                    value = ConvertFromString<T>(globals[key] as string);
                    return true;
                }
            }
            catch
            {   
                // Just return false in case of errors. If there is some garbage in the solution, falback to the default.
            }

            return false;
        }

        private void TrySetValue<T>([NotNull] EnvDTE.Globals globals, string internalKey, T value)
        {
            Contract.Requires(globals != null);

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
        private static string GetKey(string propertyName)
        {
            return @"RESX_" + propertyName;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
