namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    /// <summary>
    /// Handle global persistence.
    /// </summary>
    public abstract class ConfigurationBase : ObservableObject
    {
        [NotNull]
        private readonly ITracer _tracer;
        private const string FileName = "Configuration.xml";
        [NotNull]
        // ReSharper disable once AssignNullToNotNullAttribute
        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ResXManager");

        [NotNull]
        private readonly string _filePath;
        [NotNull]
        private readonly XmlConfiguration _configuration;

        protected ConfigurationBase([NotNull] ITracer tracer)
        {
            Contract.Requires(tracer != null);

            Contract.Assume(!string.IsNullOrEmpty(_directory));

            _tracer = tracer;
            _filePath = Path.Combine(_directory, FileName);

            try
            {
                Directory.CreateDirectory(_directory);

                using (var reader = new StreamReader(File.OpenRead(_filePath)))
                {
                    _configuration = new XmlConfiguration(tracer, reader);
                    return;
                }
            }
            catch
            {
            }

            _configuration = new XmlConfiguration(tracer);
        }

        public abstract bool IsScopeSupported
        {
            get;
        }

        public abstract ConfigurationScope Scope
        {
            get;
        }

        [NotNull]
        protected ITracer Tracer
        {
            get
            {
                Contract.Ensures(Contract.Result<ITracer>() != null);

                return _tracer;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Required by [CallerMemberName]")]
        protected T GetValue<T>(T defaultValue, [CallerMemberName] string key = null)
        {
            try
            {
                return InternalGetValue(defaultValue, key);
            }
            catch (InvalidCastException)
            {
            }

            return defaultValue;
        }

        protected virtual T InternalGetValue<T>(T defaultValue, string key)
        {
            if (key == null)
                return defaultValue;

            return ConvertFromString<T>(_configuration.GetValue(key, ConvertToString(defaultValue)));
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Required by [CallerMemberName]")]
        protected void SetValue<T>(T value, [CallerMemberName] string key = null)
        {
            if (key == null)
                return;

            if (Equals(GetValue(key), value))
                return;

            InternalSetValue(value, key);

            OnPropertyChanged(key);
        }

        protected virtual void InternalSetValue<T>(T value, [NotNull] string key)
        {
            Contract.Requires(key != null);
            try
            {
                _configuration.SetValue(key, ConvertToString(value));

                using (var writer = new StreamWriter(File.Create(_filePath)))
                {
                    _configuration.Save(writer);
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Fatal error writing configuration file: " + _filePath + " - " + ex.Message);
            }
        }

        protected static T ConvertFromString<T>(string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var typeConverter = GetTypeConverter(typeof(T));
                    var obj = typeConverter.ConvertFromInvariantString(value);
                    return obj == null ? default(T) : (T)obj;
                }
            }
            catch (NotSupportedException)
            {
            }

            return default(T);
        }

        protected static string ConvertToString<T>(T value)
        {
            if (ReferenceEquals(value, null))
                return null;

            var typeConverter = GetTypeConverter(typeof(T));
            return typeConverter.ConvertToInvariantString(value);
        }

        [NotNull]
        private static TypeConverter GetTypeConverter([NotNull] Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<TypeConverter>() != null);

            return type.GetCustomTypeConverter() ?? TypeDescriptor.GetConverter(type);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(!string.IsNullOrEmpty(_filePath));
            Contract.Invariant(!string.IsNullOrEmpty(_directory));
        }
    }
}
