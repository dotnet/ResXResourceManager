namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Store key/value pairs in an XML stream.
    /// </summary>
    public class XmlConfiguration
    {
        /// <summary>
        /// The default namespace used in the XML file.
        /// </summary>
        public const string DefaultNamespace = "urn:tom-englert.de/Configuration/1/0";
        /// <summary>
        /// The name of the root node.
        /// </summary>
        public const string RootNodeName = "Values";

        /// <summary>
        /// The name of the value node.
        /// </summary>
        public const string ValueNodeName = "Value";

        /// <summary>
        /// The name of the key attribute.
        /// </summary>
        public const string KeyAttributeName = "Key";

        private readonly XDocument _document;
        private readonly XElement _root;
        private readonly XNamespace _namespace;
        private readonly XName _valueName;
        private readonly XName _keyName;


        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConfiguration"/> class.
        /// </summary>
        public XmlConfiguration()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConfiguration" /> class.
        /// </summary>
        /// <param name="reader">The reader providing the XML stream.</param>
        public XmlConfiguration(TextReader reader)
        {
            if ((reader != null) && (reader.Peek() != -1))
            {
                try
                {
                    _document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                    _root = _document.Root;

                    if (_root != null)
                    {
                        _namespace = _root.GetDefaultNamespace();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }

            if ((_namespace == null) || !DefaultNamespace.Equals(_namespace.NamespaceName))
            {
                _namespace = XNamespace.Get(DefaultNamespace);
                _root = new XElement(XName.Get("Values", _namespace.NamespaceName));
                _document = new XDocument(_root);
            }

            _keyName = XName.Get("Key");
            _valueName = XName.Get("Value", _namespace.NamespaceName);
        }

        /// <summary>
        /// Loads the configuration from the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>The configuration loaded from the file; an empty configuration if the file does not exist or is not accessible.</returns>
        public static XmlConfiguration Load(string fileName)
        {
            Contract.Requires(fileName != null);
            Contract.Ensures(Contract.Result<XmlConfiguration>() != null);

            if (File.Exists(fileName))
            {
                try
                {
                    using (var reader = new StreamReader(fileName))
                    {
                        return new XmlConfiguration(reader);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }

            return new XmlConfiguration(null);
        }

        /// <summary>
        /// Gets the value with the specified key from the XML stream.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value stored in the XML file, or null if the value does not exist.</returns>
        public string GetValue(string key, string defaultValue)
        {
            Contract.Requires(key != null);

            return _root.DescendantsAndSelf(_valueName)
                .Select(node => new { Node = node, KeyAttribute = node.Attribute(_keyName) })
                .Where(item => (item.KeyAttribute != null) && key.Equals(item.KeyAttribute.Value))
                .Select(item => item.Node.FirstNode as XText)
                .Where(node => node != null)
                .Select(node => node.Value)
                .FirstOrDefault() ?? defaultValue;
        }

        /// <summary>
        /// Sets the value with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value. If value is null, the node will be deleted from the xml stream.</param>
        public void SetValue(string key, string value)
        {
            Contract.Requires(key != null);

            var valueNode = _root.Descendants(_valueName)
                .FirstOrDefault(node => string.Equals(key, node.Attribute(_keyName).Value));

            if (value == null)
            {
                if (valueNode != null)
                {
                    valueNode.Remove();
                }
                return;
            }

            if (valueNode == null)
            {
                valueNode = new XElement(_valueName);
                _root.Add(valueNode);
                valueNode.Add(new XAttribute(_keyName, key));
            }

            var textNode = valueNode.FirstNode as XText;

            if (textNode == null)
            {
                valueNode.Add(new XText(value));
            }
            else
            {
                textNode.Value = value;
            }
        }

        /// <summary>
        /// Saves the XML stream to the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void Save(TextWriter writer)
        {
            Contract.Requires(writer != null);

            _document.Save(writer);
        }

        /// <summary>
        /// Saves the XML stream to the file with the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void Save(string fileName)
        {
            Contract.Requires(!string.IsNullOrEmpty(fileName));

            using (var writer = new StreamWriter(fileName))
            {
                Save(writer);
            }
        }

        public override string ToString()
        {
            return _document.ToString();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_document != null);
            Contract.Invariant(_root != null);
            Contract.Invariant(_keyName != null);
            Contract.Invariant(_valueName != null);
            Contract.Invariant(_namespace != null);
        }
    }
}
