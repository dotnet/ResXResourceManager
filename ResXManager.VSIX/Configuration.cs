namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;

    internal class Configuration : Model.Configuration
    {
        private readonly EnvDTE.DTE _dte;

        public Configuration(EnvDTE.DTE dte)
        {
            Contract.Requires(dte != null);

            _dte = dte;
        }

        public override bool IsScopeSupported
        {
            get
            {
                return true;
            }
        }

        public override ConfigurationScope Scope
        {
            get
            {
                var solution = _dte.Solution;
                
                return (solution != null) && !string.IsNullOrEmpty(solution.FullName) && (solution.Globals != null) 
                    ? ConfigurationScope.Solution
                    : ConfigurationScope.Global;
            }
        }

        protected override T GetValue<T>(Expression<Func<T>> propertyExpression, T defaultValue)
        {
            T value;

            return TryGetValue(GetKey(PropertySupport.ExtractPropertyName(propertyExpression)), out value) ? value : base.GetValue(propertyExpression, defaultValue);
        }

        private bool TryGetValue<T>(string key, out T value)
        {
            value = default(T);
            var solution = _dte.Solution;

            return (solution != null) && !string.IsNullOrEmpty(solution.FullName) && TryGetValue(solution.Globals, key, ref value);
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
            var solution = _dte.Solution;

            if ((solution != null) && !string.IsNullOrEmpty(solution.FullName) && (solution.Globals != null))
            {
                var globals = solution.Globals;
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
            return "RESX_" + propertyName;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_dte != null);
        }

    }
}
