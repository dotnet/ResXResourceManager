namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using AutoProperties;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Handle global persistence.
    /// </summary>
    public abstract class ConfigurationBase : INotifyPropertyChanged
    {
        private const string FileName = "Configuration.xml";

        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ResXManager");
        private readonly string _filePath;
        private readonly XmlConfiguration _configuration;
        private readonly Dictionary<string, object> _cachedObjects = new Dictionary<string, object>();

        protected ConfigurationBase(ITracer tracer)
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

        [InterceptIgnore]
        protected ITracer Tracer { get; }

        [return: MaybeNull]
        [GetInterceptor]
        protected T GetProperty<T>(string key, PropertyInfo? propertyInfo)
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
                return default!;

            ((INotifyChanged)value).Changed += (sender, e) =>
            {
                SetValue((T)sender, key);
            };

            _cachedObjects.Add(key, value);

            return value;
        }

        [return: MaybeNull]
        private T GetValue<T>([AllowNull] T defaultValue, string key)
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

        [return: MaybeNull]
        protected virtual T InternalGetValue<T>([AllowNull] T defaultValue, string key)
        {
            return ConvertFromString(_configuration.GetValue(key, null), defaultValue);
        }

        [SetInterceptor]
        protected void SetValue<T>([AllowNull] T value, string key)
        {
            InternalSetValue(value, key);
        }

        protected virtual void InternalSetValue<T>([AllowNull] T value, string key)
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

        [return: MaybeNull]
        protected static T ConvertFromString<T>(string? value, [AllowNull] T defaultValue)
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

        protected static string? ConvertToString<T>([AllowNull] T value)
        {
            if (value == null)
                return null;

            var typeConverter = GetTypeConverter(typeof(T));
            return typeConverter.ConvertToInvariantString(value);
        }

        private static TypeConverter GetTypeConverter(Type type)
        {
            return GetCustomTypeConverter(type) ?? TypeDescriptor.GetConverter(type);
        }

        private static TypeConverter? GetCustomTypeConverter(ICustomAttributeProvider item)
        {
            /*
             * Workaround: a copy of the identical method from TomsToolbox.Essentials.
             * Calling the original method fails when running in VS, because it can't dynamically load the assembly with the serializer types.
             */
            return item
                .GetCustomAttributes<TypeConverterAttribute>(false)
                .Select(attr => attr.ConverterTypeName)
                .Select(typeName => Type.GetType(typeName, true))
                .Where(type => typeof(TypeConverter).IsAssignableFrom(type))
                .Select(type => (TypeConverter)Activator.CreateInstance(type))
                .FirstOrDefault();
        }

        [return: MaybeNull]
        private static T GetDefaultValue<T>(MemberInfo? propertyInfo)
        {
            var defaultValueAttribute = propertyInfo?.GetCustomAttributes<DefaultValueAttribute>().Select(attr => attr?.Value).FirstOrDefault();

            switch (defaultValueAttribute)
            {
                case T defaultValue:
                    return defaultValue;
                case string stringValue:
                    return ConvertFromString(stringValue, default(T)!);
            }

            return default!;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
