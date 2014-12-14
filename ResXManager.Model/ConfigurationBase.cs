namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;

    [ContractClass(typeof(ConfigurationBaseContract))]
    public abstract class ConfigurationBase : ObservableObject
    {
        private CodeReferenceConfiguration _codeReferences;

        public CodeReferenceConfiguration CodeReferences
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                return _codeReferences ?? (_codeReferences = GetValue(() => CodeReferences) ?? CodeReferenceConfiguration.Default);
            }
        }

        public bool SortFileContentOnSave
        {
            get
            {
                return GetValue(() => SortFileContentOnSave);
            }
            set
            {
                SetValue(value, () => SortFileContentOnSave);
            }
        }

        public void PersistCodeReferences()
        {
            SetValue(CodeReferences, () => CodeReferences);
        }

        protected abstract T GetValue<T>(Expression<Func<T>> propertyExpression);

        protected abstract void SetValue<T>(T value, Expression<Func<T>> propertyExpression);

        protected static string GetKey<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return GetKey(PropertySupport.ExtractPropertyName(propertyExpression));
        }

        protected static T ConvertFromString<T>(string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var typeConverter = GetTypeConverter(typeof (T));
                    var obj = typeConverter.ConvertFromInvariantString(value);
                    return obj == null ? default(T) : (T)obj;
                }
            }
            catch (NotSupportedException)
            {
            }

            return default(T);
        }

        protected static string ConvertToString<T>(object value)
        {
            var typeConverter = GetTypeConverter(typeof(T));
            return typeConverter.ConvertToInvariantString(value);
        }

        protected static string GetKey(string propertyName)
        {
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return "RESX_" + propertyName;
        }

        private static TypeConverter GetTypeConverter(Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<TypeConverter>() != null);

            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter == null)
                throw new InvalidOperationException("No type converter found for type " + type.Name);

            return typeConverter;
        }
    }

    [ContractClassFor(typeof(ConfigurationBase))]
    abstract class ConfigurationBaseContract : ConfigurationBase
    {
        protected override T GetValue<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            throw new NotImplementedException();
        }

        protected override void SetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            throw new NotImplementedException();
        }
    }
}
