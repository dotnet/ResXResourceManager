namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    [Export(typeof(Configuration))]
    [Export(typeof(DteConfiguration))]
    internal class DteConfiguration : Configuration
    {
        private readonly DteSolution _solution;
        private readonly DispatcherThrottle _moveToResourcesChangeThrottle;
        private MoveToResourceConfiguration _moveToResources;

        [ImportingConstructor]
        public DteConfiguration(DteSolution solution, ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(solution != null);
            Contract.Requires(tracer != null);

            _solution = solution;
            _moveToResourcesChangeThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, PersistMoveToResources);
        }

        public MoveToResourceConfiguration MoveToResources
        {
            get
            {
                Contract.Ensures(Contract.Result<MoveToResourceConfiguration>() != null);

                return _moveToResources ?? CreateMoveToResourceConfiguration();
            }
        }

        private void PersistMoveToResources()
        {
            SetValue(MoveToResources, () => MoveToResources);
        }

        private MoveToResourceConfiguration CreateMoveToResourceConfiguration()
        {
            _moveToResources = GetValue(() => MoveToResources) ?? MoveToResourceConfiguration.Default;
            _moveToResources.ItemPropertyChanged += (_, __) => _moveToResourcesChangeThrottle.Tick();

            return _moveToResources;
        }

        public override bool IsScopeSupported => true;

        public override ConfigurationScope Scope => (_solution.Globals != null) ? ConfigurationScope.Solution : ConfigurationScope.Global;

        protected override T GetValue<T>(Expression<Func<T>> propertyExpression, T defaultValue)
        {
            T value;

            return TryGetValue(GetKey(PropertySupport.ExtractPropertyName(propertyExpression)), out value) ? value : base.GetValue(propertyExpression, defaultValue);
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
                if ((globals != null) && (globals.VariableExists[key]))
                {
                    value = ConvertFromString<T>(globals[key] as string);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        protected override void InternalSetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            var globals = _solution.Globals;

            if (globals != null)
            {
                var propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
                var key = GetKey(propertyName);

                globals[key] = ConvertToString<T>(value);
                globals.VariablePersists[key] = true;

                OnPropertyChanged(propertyName);
            }
            else
            {
                base.InternalSetValue(value, propertyExpression);
            }
        }

        private static string GetKey(string propertyName)
        {
            return @"RESX_" + propertyName;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
        }
    }
}
