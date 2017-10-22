namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

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

        [NotNull]
        private readonly XDocument _document;
        [NotNull]
        private readonly XElement _root;
        [NotNull]
        private readonly XName _valueName;
        [NotNull]
        private readonly XName _keyName;


        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConfiguration" /> class.
        /// </summary>
        /// <param name="tracer">The tracer.</param>
        public XmlConfiguration([NotNull] ITracer tracer)
            : this(tracer, null)
        {
            Contract.Requires(tracer != null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConfiguration" /> class.
        /// </summary>
        /// <param name="tracer">The tracer.</param>
        /// <param name="reader">The reader providing the XML stream.</param>
        public XmlConfiguration([NotNull] ITracer tracer, [CanBeNull] TextReader reader)
        {
            Contract.Requires(tracer != null);

            XElement root = null;
            XNamespace @namespace = null;
            XDocument document = null;

            if ((reader != null) && (reader.Peek() != -1))
            {
                try
                {
                    document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                    root = document.Root;

                    if (root != null)
                    {
                        @namespace = root.GetDefaultNamespace();
                    }
                }
                catch (Exception ex)
                {
                    tracer.TraceError(ex.ToString());
                }
            }

            if ((@namespace == null) || !DefaultNamespace.Equals(@namespace.NamespaceName))
            {
                @namespace = XNamespace.Get(DefaultNamespace);
                root = new XElement(XName.Get("Values", @namespace.NamespaceName));
                document = new XDocument(root);
            }

            _document = document;
            _root = root;
            _keyName = XName.Get("Key");
            _valueName = XName.Get("Value", @namespace.NamespaceName);
        }

        /// <summary>
        /// Gets the value with the specified key from the XML stream.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// The value stored in the XML file, or null if the value does not exist.
        /// </returns>
        [CanBeNull]
        public string GetValue([NotNull] string key, [CanBeNull] string defaultValue)
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
        public void SetValue([NotNull] string key, [CanBeNull] string value)
        {
            Contract.Requires(key != null);

            var valueNode = _root.Descendants(_valueName)
                .FirstOrDefault(node => string.Equals(key, node.Attribute(_keyName).Value));

            if (value == null)
            {
                valueNode?.Remove();
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
        public void Save([NotNull] TextWriter writer)
        {
            Contract.Requires(writer != null);

            _document.Save(writer);
        }

        /// <summary>
        /// Saves the XML stream to the file with the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void Save([NotNull] string fileName)
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
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_document != null);
            Contract.Invariant(_root != null);
            Contract.Invariant(_keyName != null);
            Contract.Invariant(_valueName != null);
        }
    }
}
