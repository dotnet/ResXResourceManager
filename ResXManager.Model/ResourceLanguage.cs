namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Xml;
    using System.Xml.Linq;
    using tomenglertde.ResXManager.Model.Properties;

    /// <summary>
    /// Represents a set of localized resources.
    /// </summary>
    [Localizable(false)]
    public class ResourceLanguage : IEquatable<ResourceLanguage>
    {
        private const string Quote = "\"";
        private const string DataElementTemplate = "<data name=\"{0}\" xml:space=\"preserve\"/>";

        private readonly XDocument _document;
        private readonly XElement _documentRoot;
        private readonly ProjectFile _file;
        private readonly IEnumerable<XElement> _data;
        private readonly IDictionary<string, Node> _nodes;
        private readonly CultureKey _cultureKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLanguage" /> class.
        /// </summary>
        /// <param name="cultureKey">The culture key.</param>
        /// <param name="file">The .resx file having all the localization.</param>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        internal ResourceLanguage(CultureKey cultureKey, ProjectFile file)
        {
            Contract.Requires(cultureKey != null);
            Contract.Requires(file != null);

            _cultureKey = cultureKey;
            _file = file;

            try
            {
                _document = XDocument.Load(file.FilePath);
                _documentRoot = _document.Root;
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath), ex);
            }

            if (_documentRoot == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath));

            _data = _documentRoot.Elements(@"data");

            var elements = _data
                .Where(IsStringType)
                .Select(item => new Node(this, item))
                .Where(item => !item.Key.StartsWith(@">>", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            try
            {
                _nodes = elements.ToDictionary(item => item.Key);
            }
            catch (ArgumentException ex)
            {
                var duplicateKeys = string.Join(@", ", elements.GroupBy(item => item.Key).Where(group => group.Count() > 1).Select(group => Quote + group.Key + Quote));
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateKeyError, file.FilePath, duplicateKeys), ex);
            }
        }

        public event EventHandler<CancelEventArgs> Changing;
        public event EventHandler Changed;

        /// <summary>
        /// Gets the culture of this language.
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                return _cultureKey.Culture;
            }
        }

        /// <summary>
        /// Gets the display name of this language.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Culture.Maybe().Return(l => l.DisplayName, Resources.Neutral);
            }
        }

        /// <summary>
        /// Gets all the resource keys defined in this language.
        /// </summary>
        public IEnumerable<string> ResourceKeys
        {
            get
            {
                return _nodes.Keys;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the file associated with this instance can be written.
        /// </summary>
        public bool IsWritable
        {
            get
            {
                try
                {
                    if ((File.GetAttributes(_file.FilePath) & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                        return false;

                    using (File.Open(_file.FilePath, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                return false;
            }
        }

        public bool HasChanges
        {
            get;
            private set;
        }

        public string FileName
        {
            get
            {
                Contract.Ensures(!String.IsNullOrEmpty(Contract.Result<string>()));
                return _file.FilePath;
            }
        }

        public ProjectFile ProjectFile
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectFile>() != null);
                return _file;
            }
        }

        public bool IsNeutralLanguage
        {
            get;
            internal set;
        }

        public CultureKey CultureKey
        {
            get
            {
                return _cultureKey;
            }
        }

        private static bool IsStringType(XElement entry)
        {
            Contract.Requires(entry != null);

            var typeAttribute = entry.Attribute(@"type");

            if (typeAttribute != null)
            {
                return string.IsNullOrEmpty(typeAttribute.Value) || typeAttribute.Value.StartsWith(typeof(string).Name, StringComparison.OrdinalIgnoreCase);
            }

            var mimeTypeAttribute = entry.Attribute(@"mimetype");

            return mimeTypeAttribute == null;
        }

        internal string GetValue(string key)
        {
            Node node;

            if (!_nodes.TryGetValue(key, out node) || (node == null))
                return null;

            return node.Text;
        }

        internal bool SetValue(string key, string value)
        {
            Contract.Requires(key != null);

            if (GetValue(key) == value)
                return true;

            return SetNodeData(key, node => node.Text = value);
        }

        public void ForceValue(string key, string value)
        {
            Contract.Requires(key != null);

            SetNodeData(key, node => node.Text = value);
        }

        private void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        private bool OnChanging()
        {
            if (Changing != null)
            {
                var eventArgs = new CancelEventArgs();
                Changing(this, eventArgs);
                return !eventArgs.Cancel;
            }

            return true;
        }

        /// <summary>
        /// Saves this instance to the resource file.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            _document.Save(_file.FilePath);

            HasChanges = false;
        }

        internal string GetComment(string key)
        {
            Node node;

            if (!_nodes.TryGetValue(key, out node) || (node == null))
                return null;

            return node.Comment;
        }

        internal bool SetComment(string key, string value)
        {
            Contract.Requires(key != null);

            if (GetComment(key) == value)
                return true;

            return SetNodeData(key, node => node.Comment = value);
        }

        private bool SetNodeData(string key, Action<Node> updateCallback)
        {
            Contract.Requires(key != null);
            Contract.Requires(updateCallback != null);

            if (!OnChanging())
                return false;

            try
            {
                Node node;

                if (!_nodes.TryGetValue(key, out node) || (node == null))
                {
                    node = CreateNode(key);
                }

                updateCallback(node);

                if (!IsNeutralLanguage)
                {
                    if (string.IsNullOrEmpty(node.Text) && string.IsNullOrEmpty(node.Comment))
                    {
                        node.Element.Remove();
                        _nodes.Remove(key);
                    }
                }

                HasChanges = true;
                OnChanged();

                return true;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _file.FilePath, ex.Message);
                MessageBox.Show(message);
                throw new IOException(message, ex);
            }
        }

        private Node CreateNode(string key)
        {
            Node node;
            var content = new XElement(@"value");
            content.Add(new XText(string.Empty));
            // create from a string template to be able to add the xml:space="preserve"
            var entry = XElement.Parse(string.Format(CultureInfo.InvariantCulture, DataElementTemplate, key));
            entry.Add(content);

            _documentRoot.Add(entry);
            _nodes.Add(key, node = new Node(this, entry));
            return node;
        }

        internal bool RenameKey(string oldKey, string newKey)
        {
            Node node;

            if (!OnChanging())
                return false;

            if (!_nodes.TryGetValue(oldKey, out node) || (node == null))
                return false;

            if (_nodes.ContainsKey(newKey))
                return false;

            _nodes.Remove(oldKey);
            node.Key = newKey;
            _nodes.Add(newKey, node);

            HasChanges = true;
            OnChanged();
            return true;
        }

        internal bool RemoveKey(string key)
        {
            if (!OnChanging())
                return false;

            try
            {
                Node node;

                if (!_nodes.TryGetValue(key, out node) || (node == null))
                {
                    return false;
                }

                node.Element.Remove();
                _nodes.Remove(key);

                HasChanges = true;
                OnChanged();
                return true;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _file.FilePath, ex.Message);
                MessageBox.Show(message);
                throw new IOException(message, ex);
            }
        }

        internal bool CanChange()
        {
            return OnChanging();
        }

        internal bool KeyExists(string key)
        {
            return _nodes.ContainsKey(key);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        class Node
        {
            private readonly ResourceLanguage _owner;
            private readonly XElement _element;
            private string _text;
            private string _comment;

            public Node(ResourceLanguage owner, XElement element)
            {
                Contract.Requires(element != null);
                _element = element;
                _owner = owner;
            }

            public XElement Element
            {
                get
                {
                    Contract.Ensures(Contract.Result<XElement>() != null);
                    return _element;
                }
            }

            public string Key
            {
                get
                {
                    Contract.Ensures(Contract.Result<string>() != null);

                    return GetNameAttribute(_element).Value;
                }
                set
                {
                    Contract.Requires(value != null);

                    GetNameAttribute(_element).Value = value;
                }
            }

            public string Text
            {
                get
                {
                    return _text ?? (_text = LoadText());
                }
                set
                {
                    _text = value ?? string.Empty;

                    var entry = Element;

                    var valueElement = entry.Element(@"value");
                    if (valueElement == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileValueAttributeMissingError, _owner.FileName));

                    if (valueElement.FirstNode == null)
                    {
                        valueElement.Add(value);
                    }
                    else
                    {
                        valueElement.FirstNode.ReplaceWith(value);
                    }
                }
            }

            public string Comment
            {
                get
                {
                    return _comment ?? (_comment = LoadComment());
                }
                set
                {
                    _comment = value ?? string.Empty;

                    var entry = Element;

                    var valueElement = entry.Element(@"comment");

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        if (valueElement != null)
                        {
                            valueElement.Remove();
                        }
                    }
                    else
                    {
                        if (valueElement == null)
                        {
                            valueElement = new XElement(@"comment");
                            entry.Add(valueElement);
                        }

                        var textNode = valueElement.FirstNode as XText;
                        if (textNode == null)
                        {
                            textNode = new XText(value);
                            valueElement.Add(textNode);
                        }
                        else
                        {
                            textNode.Value = value;
                        }
                    }
                }
            }

            private string LoadText()
            {
                var entry = Element;

                var valueElement = entry.Element(@"value");
                if (valueElement == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileValueAttributeMissingError, _owner.FileName));
                }

                var textNode = valueElement.FirstNode as XText;

                return textNode == null ? string.Empty : textNode.Value;
            }

            private string LoadComment()
            {
                var entry = Element;

                var valueElement = entry.Element(@"comment");
                if (valueElement == null)
                    return string.Empty;

                var textNode = valueElement.FirstNode as XText;

                return textNode == null ? string.Empty : textNode.Value;
            }

            private XAttribute GetNameAttribute(XElement entry)
            {
                Contract.Requires(entry != null);
                Contract.Ensures(Contract.Result<XAttribute>() != null);

                var nameAttribute = entry.Attribute(@"name");
                if (nameAttribute == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileNameAttributeMissingError, _owner._file.FilePath));
                }

                return nameAttribute;
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_element != null);
            }
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return CultureKey.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ResourceLanguage);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ResourceLanguage" /> is equal to this instance.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="ResourceLanguage" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ResourceLanguage other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ResourceLanguage left, ResourceLanguage right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return Equals(left.CultureKey, right.CultureKey);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ResourceLanguage left, ResourceLanguage right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ResourceLanguage left, ResourceLanguage right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_data != null);
            Contract.Invariant(_document != null);
            Contract.Invariant(_documentRoot != null);
            Contract.Invariant(_file != null);
            Contract.Invariant(_nodes != null);
            Contract.Invariant(_cultureKey != null);
        }
    }
}
