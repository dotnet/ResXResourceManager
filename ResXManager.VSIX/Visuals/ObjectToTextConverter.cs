namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Data;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Takes an object and returns the text taken from it's <see cref="TextAttribute"/>
    /// </summary>
    /// <example><code>
    /// enum Items
    /// {
    ///     [Text("key2", "This is other text on item 1")]
    ///     [Text("key1", "This is item 1")]
    ///     Item1,
    ///     [Text("key1", "This is item 2")]
    ///     Item2
    /// }
    ///
    /// Assert.Equals("This is item 1", ObjectToTextConverter.Convert("key1", Items.Item1));
    /// </code></example>
    /// <remarks>
    /// Works with any object; for enum types the attribute of the field is returned.<para/>
    /// When used via the <see cref="IValueConverter"/> interface, the key can be specified as the converter parameter.
    /// </remarks>
    [Obsolete("Remove after toolbox is updated.")]
    public class ObjectToTextConverter : ObjectToAttributeConverter<TextAttribute>
    {
        /// <summary>
        /// The singleton instance of the converter.
        /// </summary>
        public static readonly IValueConverter Default = new ObjectToTextConverter();

        /// <summary>
        /// Gets or sets the key used to select the <see cref="TextAttribute"/>;
        /// can be overwritten with the converter parameter.
        /// </summary>
        public object Key
        {
            get;
            set;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? string.Empty : Convert(parameter ?? Key, value, null);
        }

        /// <summary>
        /// Converts the specified value to the text taken from it's <see cref="TextAttribute" />
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The text of the value.</returns>
        public static string Convert(object key, object value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return InternalConvert(value, null, attr => attr.Text, attr => Equals(attr.Key, key));
        }

        /// <summary>
        /// Converts the specified value to the text taken from it's <see cref="TextAttribute" />
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="enumType">An optional type of an enum to support converting <see cref="Enum"/> where the value is given as a number or string.</param>
        /// <returns>The text of the value.</returns>
        public static string Convert(object key, object value, Type enumType)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return InternalConvert(value, enumType, attr => attr.Text, attr => Equals(attr.Key, key));
        }
    }
}
