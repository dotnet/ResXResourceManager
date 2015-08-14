namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq.Expressions;
    using System.Windows;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Handle global persistence.
    /// </summary>
    public abstract class ConfigurationBase : ObservableObject
    {
        private const string FileName = "Configuration.xml";
        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ResXManager");

        private readonly string _filePath;
        private readonly XmlConfiguration _configuration;

        public ConfigurationBase()
        {
            Contract.Assume(!string.IsNullOrEmpty(_directory));

            _filePath = Path.Combine(_directory, FileName);

            try
            {
                Directory.CreateDirectory(_directory);

                using (var reader = new StreamReader(File.OpenRead(_filePath)))
                {
                    _configuration = new XmlConfiguration(reader);
                    return;
                }
            }
            catch
            {
            }

            _configuration = new XmlConfiguration();
        }

        public abstract bool IsScopeSupported
        {
            get;
        }

        public abstract ConfigurationScope Scope
        {
            get;
        }

        protected virtual T GetValue<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            return GetValue(propertyExpression, default(T));
        }

        protected virtual T GetValue<T>(Expression<Func<T>> propertyExpression, T defaultValue)
        {
            Contract.Requires(propertyExpression != null);

            var key = PropertySupport.ExtractPropertyName(propertyExpression);

            try
            {
                return ConvertFromString<T>(_configuration.GetValue(key, defaultValue as string));
            }
            catch (InvalidCastException)
            {
            }
            return defaultValue;
        }

        protected void SetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            if (Equals(GetValue(propertyExpression), value))
                return;

            InternalSetValue(value, propertyExpression);
        }

        protected virtual void InternalSetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            var propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            var key = propertyName;

            try
            {
                _configuration.SetValue(key, ConvertToString<T>(value));

                using (var writer = new StreamWriter(File.Create(_filePath)))
                {
                    _configuration.Save(writer);
                }

                OnPropertyChanged(propertyName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error writing configuration file: " + _filePath + " - " + ex.Message);
            }
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

        private static TypeConverter GetTypeConverter(Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<TypeConverter>() != null);

            return type.GetCustomTypeConverter() ?? TypeDescriptor.GetConverter(type);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_configuration != null);
            Contract.Invariant(!String.IsNullOrEmpty(_filePath));
            Contract.Invariant(!String.IsNullOrEmpty(_directory));
        }
    }
}
