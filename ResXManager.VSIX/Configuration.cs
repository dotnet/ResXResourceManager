namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using EnvDTE;
    using tomenglertde.ResXManager.Model;

    internal class Configuration : ConfigurationBase
    {
        private readonly EnvDTE.DTE _dte;

        public Configuration(EnvDTE.DTE dte)
        {
            Contract.Requires(dte != null);

            _dte = dte;
        }

        protected override T GetValue<T>(Expression<Func<T>> propertyExpression)
        {
            var key = GetKey(propertyExpression);

            return GetValue<T>(key);
        }

        private T GetValue<T>(string key)
        {
            T value;
            var solution = _dte.Solution;

            if ((solution != null) && !string.IsNullOrEmpty(solution.FullName) && TryGetValue(solution.Globals, key, out value))
                return value;

            if (TryGetValue(_dte.Globals, key, out value))
                return value;

            return default(T);
        }


        private bool TryGetValue<T>(EnvDTE.Globals globals, string key, out T value)
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

            value = default(T);
            return false;
        }

        protected override void SetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            var propertyName = PropertySupport.ExtractPropertyName(propertyExpression);

            var key = GetKey(propertyName);

            if (Equals(GetValue<T>(key), value))
                return;

            var solution = _dte.Solution;

            var globals = (solution != null) && !string.IsNullOrEmpty(solution.FullName) ? solution.Globals : _dte.Globals;

            Contract.Assume(globals != null);
            SetValue(globals, key, ConvertToString<T>(value));

            OnPropertyChanged(propertyName);
        }

        private static void SetValue(EnvDTE.Globals globals, string key, string value)
        {
            Contract.Requires(globals != null);

            globals[key] = value;
            globals.VariablePersists[key] = true;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_dte != null);
        }

    }
}
