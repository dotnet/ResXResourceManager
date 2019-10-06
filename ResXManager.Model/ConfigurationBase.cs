namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;

    using AutoProperties;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Handle global persistence.
    /// </summary>
    public abstract class ConfigurationBase : INotifyPropertyChanged
    {
        private const string FileName = "Configuration.xml";

        [NotNull]
        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ResXManager");
        [NotNull]
        private readonly string _filePath;
        [NotNull]
        private readonly XmlConfiguration _configuration;
        [NotNull]
        private readonly Dictionary<string, object> _cachedObjects = new Dictionary<string, object>();

        protected ConfigurationBase([NotNull] ITracer tracer)
        {
            Tracer = tracer;
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
                // can't read configuration, just go with default.
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

        [NotNull, InterceptIgnore]
        protected ITracer Tracer { get; }

        [CanBeNull, GetInterceptor, UsedImplicitly]
        protected T GetProperty<T>([NotNull] string key, [CanBeNull] PropertyInfo propertyInfo)
        {
            if (!typeof(INotifyChanged).IsAssignableFrom(typeof(T)))
            {
                return GetValue(GetDefaultValue<T>(propertyInfo), key);
            }

            if (_cachedObjects.TryGetValue(key, out var item))
            {
                return (T)item;
            }

            var value = GetValue(GetDefaultValue<T>(propertyInfo), key);

            if (value == null)
                return default;

            ((INotifyChanged)value).Changed += (sender, e) =>
            {
                SetValue((T)sender, key);
            };

            _cachedObjects.Add(key, value);

            return value;
        }

        [CanBeNull]
        private T GetValue<T>([CanBeNull] T defaultValue, [NotNull] string key)
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

        [CanBeNull]
        protected virtual T InternalGetValue<T>([CanBeNull] T defaultValue, [NotNull] string key)
        {
            return ConvertFromString(_configuration.GetValue(key, null), defaultValue);
        }

        [SetInterceptor, UsedImplicitly]
        protected void SetValue<T>([CanBeNull] T value, [NotNull] string key)
        {
            InternalSetValue(value, key);
        }

        protected virtual void InternalSetValue<T>([CanBeNull] T value, [NotNull] string key)
        {
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
                Tracer.TraceError("Fatal error writing configuration file: " + _filePath + " - " + ex.Message);
            }
        }

        [CanBeNull]
        protected T ConvertFromString<T>([CanBeNull] string value, [CanBeNull] T defaultValue)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var typeConverter = GetTypeConverter(typeof(T));
                    var obj = typeConverter.ConvertFromInvariantString(value);
                    return obj == null ? defaultValue : (T)obj;
                }
            }
            catch (NotSupportedException)
            {
            }

            return defaultValue;
        }

        [CanBeNull]
        protected string ConvertToString<T>([CanBeNull] T value)
        {
            if (ReferenceEquals(value, null))
                return null;

            var typeConverter = GetTypeConverter(typeof(T));
            return typeConverter.ConvertToInvariantString(value);
        }

        [NotNull]
        private TypeConverter GetTypeConverter([NotNull] Type type)
        {
            return GetCustomTypeConverter(type) ?? TypeDescriptor.GetConverter(type);
        }

        [CanBeNull]
        private TypeConverter GetCustomTypeConverter([NotNull] ICustomAttributeProvider item)
        {
            /*
             * Workaround: a copy of the identical method from TomsToolbox.Essentials.
             * Calling the original method fails when running in VS!
             */

            var a = item.GetCustomTypeConverter(out var log1);

            var logBuilder = new StringBuilder();

            var b = item
                .GetCustomAttributes<TypeConverterAttribute>(false)
                .ToList().Intercept(i => logBuilder.AppendLine($"# of TypeConverterAttributes: {i?.Count}"))
                .Select(attr => attr.ConverterTypeName)
                .ToList().Intercept(i => logBuilder.AppendLine($"Type names: {string.Join("; ", i)}"))
                .Select(Type.GetType)
                .Where(type => (type != null))
                .ToList().Intercept(i => logBuilder.AppendLine($"Types: {string.Join("; ", i)}"))
                .Where(type => typeof(TypeConverter).IsAssignableFrom(type))
                .ToList().Intercept(i => logBuilder.AppendLine($"Type converters: {string.Join("; ", i)}"))
                .Select(type => (TypeConverter)Activator.CreateInstance(type))
                .FirstOrDefault();

            var log2 = logBuilder.ToString();

            if (a?.GetType() != b?.GetType())
            {
                Tracer.TraceWarning("GetCustomTypeConverter: \r\n- " + log1 + "\r\n -" + log2);
            }

            return b;
        }

        [CanBeNull]
        private T GetDefaultValue<T>([CanBeNull] MemberInfo propertyInfo)
        {
            var defaultValueAttribute = propertyInfo?.GetCustomAttributes<DefaultValueAttribute>().Select(attr => attr?.Value).FirstOrDefault();

            switch (defaultValueAttribute)
            {
                case T defaultValue:
                    return defaultValue;
                case string stringValue:
                    return ConvertFromString(stringValue, default(T));
            }

            return default;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
