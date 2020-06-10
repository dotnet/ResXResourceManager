namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using AutoProperties;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

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
        protected T GetProperty<T>([NotNull] string key, PropertyInfo? propertyInfo)
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
        protected static T ConvertFromString<T>(string? value, [CanBeNull] T defaultValue)
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

        protected static string? ConvertToString<T>([CanBeNull] T value)
        {
            if (value == null)
                return null;

            var typeConverter = GetTypeConverter(typeof(T));
            return typeConverter.ConvertToInvariantString(value);
        }

        [NotNull]
        private static TypeConverter GetTypeConverter([NotNull] Type type)
        {
            return GetCustomTypeConverter(type) ?? TypeDescriptor.GetConverter(type);
        }

        private static TypeConverter? GetCustomTypeConverter([NotNull] ICustomAttributeProvider item)
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

        [CanBeNull]
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
