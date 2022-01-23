namespace ResXManager.Model;

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

using TomsToolbox.Essentials;

/// <summary>
/// A type converter that converts an integer string to a boolean value.
/// </summary>
/// <remarks>
/// 0 means false, any other integer value true.
/// If the string can not be parsed, false.
/// </remarks>
public class JsonSerializerTypeConverter<T> : TypeConverter
    where T : class
{
    private readonly DataContractJsonSerializer _serializer = new(typeof(T));

    /// <summary>
    /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
    /// </summary>
    /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
    /// <param name="sourceType">A <see cref="Type"/> that represents the type you want to convert from.</param>
    /// <returns>
    /// true if this converter can perform the conversion; otherwise, false.
    /// </returns>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <summary>
    /// Returns whether this converter can convert the object to the specified type, using the specified context.
    /// </summary>
    /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
    /// <param name="destinationType">A <see cref="Type"/> that represents the type you want to convert to.</param>
    /// <returns>
    /// true if this converter can perform the conversion; otherwise, false.
    /// </returns>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <summary>
    /// Converts the given object to the type of this converter, using the specified context and culture information.
    /// </summary>
    /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
    /// <param name="culture">The <see cref="CultureInfo"/> to use as the current culture.</param>
    /// <param name="value">The <see cref="object"/> to convert.</param>
    /// <returns>
    /// An <see cref="object"/> that represents the converted value.
    /// </returns>
    /// <exception cref="NotSupportedException">The conversion cannot be performed. </exception>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo culture, object? value)
    {
        if (value is not string stringValue)
            return null;

        try
        {
            if (!stringValue.IsNullOrEmpty())
            {
                using (var stream = new MemoryStream())
                {
                    var data = Encoding.Default.GetBytes(stringValue);
                    stream.Write(data, 0, data.Length);
                    stream.Seek(0, SeekOrigin.Begin);

                    return _serializer.ReadObject(stream);
                }
            }
        }
        catch (SerializationException)
        {
        }

        return default(T);
    }

    /// <summary>
    /// Converts the given value object to the specified type, using the specified context and culture information.
    /// </summary>
    /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
    /// <param name="culture">A <see cref="CultureInfo"/>. If null is passed, the current culture is assumed.</param>
    /// <param name="value">The <see cref="object"/> to convert.</param>
    /// <param name="destinationType">The <see cref="Type"/> to convert the <paramref name="value"/> parameter to.</param>
    /// <returns>
    /// An <see cref="object"/> that represents the converted value.
    /// </returns>
    /// <exception cref="ArgumentNullException">The <paramref name="destinationType"/> parameter is null. </exception>
    /// <exception cref="NotSupportedException">The conversion cannot be performed. </exception>
    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object value, Type? destinationType)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (value.GetType() != typeof(T))
            throw new ArgumentException(typeof(T).Name + @" expected", nameof(value));

        if (destinationType != typeof(string))
            throw new InvalidOperationException(@"Only string conversion is supported.");

        using (var stream = new MemoryStream())
        {
            _serializer.WriteObject(stream, value);

            return Encoding.Default.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }
    }
}
