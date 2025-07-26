namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

using AutoProperties;

using PropertyChanged;

using ResXManager.Infrastructure;

using Throttle;

using TomsToolbox.Essentials;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForceGlobalAttribute : Attribute
{
}

/// <summary>
/// Handle global persistence.
/// </summary>
public abstract class ConfigurationBase : INotifyPropertyChanged
{
    private const string GlobalConfigFileName = "Configuration.xml";
    private const string SolutionConfigFileName = "ResXManager.config.xml";

    private static readonly string _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ResXManager");

    private readonly string _globalConfigFilePath;
    private readonly XmlConfiguration _globalConfiguration;
    private readonly Dictionary<string, object> _cachedObjects = new();

    private XmlConfiguration? _solutionConfiguration;
    private string? _solutionConfigFilePath;
    private readonly PropertyInfo[] _allPublicProperties;

    protected ConfigurationBase(ITracer tracer)
    {
        Tracer = tracer;
        _allPublicProperties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        _globalConfigFilePath = Path.Combine(_appDataFolder, GlobalConfigFileName);
        _globalConfiguration = LoadConfiguration(_globalConfigFilePath, tracer);

        tracer.WriteLine("Configuration loaded.");
    }

    [InterceptIgnore]
    public ConfigurationScope Scope { get; private set; }

    [InterceptIgnore]
    public XmlConfiguration EffectiveConfiguration => _solutionConfiguration ?? _globalConfiguration;

    [InterceptIgnore]
    protected ITracer Tracer { get; }

    [InterceptIgnore]
    [OnChangedMethod(nameof(OnSolutionFolderChanged))]
    public string? SolutionFolder { get; set; }

    [GetInterceptor]
    protected T? GetProperty<T>(string key, PropertyInfo propertyInfo)
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
            SetValue((T?)sender, key, propertyInfo);
        };

        _cachedObjects.Add(key, value);

        return value;
    }

    private T? GetValue<T>(T? defaultValue, string key)
    {
        try
        {
            return InternalGetValue(defaultValue, key);
        }
        catch (Exception)
        {
            // input value is invalid, ignore and just go with the default.
        }

        return defaultValue;
    }

    protected virtual T? InternalGetValue<T>(T? defaultValue, string key)
    {
        return ConvertFromString(_solutionConfiguration?.GetValue(key, null) ?? _globalConfiguration.GetValue(key, null), defaultValue);
    }

    [SetInterceptor]
    protected void SetValue<T>(T? value, string key, PropertyInfo property)
    {
        var forceGlobal = property.GetCustomAttributes<ForceGlobalAttribute>().Any();

        InternalSetValue(value, key, forceGlobal);
    }

    protected virtual void InternalSetValue<T>(T? value, string key, bool forceGlobal)
    {
        try
        {
            var configuration = forceGlobal ? _globalConfiguration : EffectiveConfiguration;

            configuration.SetValue(key, ConvertToString(value));

            Save();
        }
        catch (Exception ex)
        {
            Tracer.TraceError($"Error converting configuration value: {key} - {ex.Message}");
        }
    }

    [Throttled(typeof(AsyncThrottle))]
    private void Save()
    {
        Save(_globalConfiguration, _globalConfigFilePath);
        Save(_solutionConfiguration, _solutionConfigFilePath);
    }

    private void Save(XmlConfiguration? configuration, string? filePath)
    {
        if (configuration == null || filePath.IsNullOrEmpty())
            return;

        Tracer.WriteLine("Saving configuration to {0}", filePath);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using var writer = new StreamWriter(File.Create(filePath));
            configuration.Save(writer);
        }
        catch (Exception ex)
        {
            Tracer.TraceError($"Fatal error writing configuration file: {filePath} - {ex.Message}");
        }
    }

    protected static T? ConvertFromString<T>(string? value, T? defaultValue)
    {
        try
        {
            if (!value.IsNullOrEmpty())
            {
                var typeConverter = GetTypeConverter(typeof(T));
                var obj = typeConverter.ConvertFromInvariantString(value);
                return obj == null ? defaultValue : (T)obj;
            }
        }
        catch (Exception)
        {
            // input value is invalid, ignore and just go with the default.
        }

        return defaultValue;
    }

    protected static string? ConvertToString<T>(T? value)
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
            .ExceptNullItems()
            .Where(type => typeof(TypeConverter).IsAssignableFrom(type))
            .Select(type => (TypeConverter?)Activator.CreateInstance(type))
            .FirstOrDefault();
    }

    private static T? GetDefaultValue<T>(MemberInfo? propertyInfo)
    {
        var defaultValueAttribute = propertyInfo?.GetCustomAttributes<DefaultValueAttribute>().Select(attr => attr?.Value).FirstOrDefault();

        return defaultValueAttribute switch
        {
            T defaultValue => defaultValue,
            string stringValue => ConvertFromString(stringValue, default(T)),
            _ => default,
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private void OnSolutionFolderChanged(string oldValue, string newValue)
    {
        if (newValue.IsNullOrEmpty())
        {
            _solutionConfigFilePath = null;
            _solutionConfiguration = null;
            Scope = ConfigurationScope.Global;
        }
        else
        {
            _solutionConfigFilePath = Path.Combine(newValue, SolutionConfigFileName);
            _solutionConfiguration = LoadConfiguration(_solutionConfigFilePath, Tracer);
            Scope = ConfigurationScope.Solution;
        }

        Tracer.WriteLine("Solution folder changed: {0} ({1})", newValue, Scope);

        NotifyAll();
    }

    private static XmlConfiguration LoadConfiguration(string filePath, ITracer tracer)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using var reader = new StreamReader(File.OpenRead(filePath));

            return new(tracer, reader);
        }
        catch
        {
            // can't read configuration, just go with default.
        }

        return new(tracer);
    }

    private void NotifyAll()
    {
        foreach (var property in _allPublicProperties)
        {
            OnPropertyChanged(property.Name);
        }
    }
}
