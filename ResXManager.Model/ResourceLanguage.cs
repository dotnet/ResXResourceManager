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

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Core;

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
        private readonly IDictionary<string, Node> _nodes;
        private readonly ResourceManager _resourceManager;
        private readonly CultureKey _cultureKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLanguage" /> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        /// <param name="cultureKey">The culture key.</param>
        /// <param name="file">The .resx file having all the localization.</param>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        internal ResourceLanguage(ResourceManager resourceManager, CultureKey cultureKey, ProjectFile file)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(cultureKey != null);
            Contract.Requires(file != null);

            _resourceManager = resourceManager;
            _cultureKey = cultureKey;
            _file = file;

            try
            {
                _document = XDocument.Parse(file.Content);
                _documentRoot = _document.Root;
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath), ex);
            }

            if (_documentRoot == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath));

            var data = _documentRoot.Elements(@"data");

            var elements = data
                .Where(IsStringType)
                .Select(item => new Node(this, item))
                .Where(item => !item.Key.StartsWith(@">>", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (_resourceManager.Configuration.DuplicateKeyHandling == DuplicateKeyHandling.Rename)
            {
                MakeKeysUnique(elements);
            }

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
                Contract.Ensures(Contract.Result<string>() != null);

                return Culture.Maybe().Return(l => l.DisplayName) ?? Resources.Neutral;
            }
        }

        /// <summary>
        /// Gets all the resource keys defined in this language.
        /// </summary>
        public IEnumerable<string> ResourceKeys
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return _nodes.Keys;
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
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

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
                Contract.Ensures(Contract.Result<CultureKey>() != null);

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
            Contract.Requires(key != null);

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

        public void SortNodesByKey()
        {
            Save(true);
        }

        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private bool OnChanging()
        {
            var handler = Changing;
            if (handler == null)
                return true;

            var eventArgs = new CancelEventArgs();
            handler(this, eventArgs);
            return !eventArgs.Cancel;
        }

        /// <summary>
        /// Saves this instance to the resource file.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            Save(false);
        }

        /// <summary>
        /// Saves this instance to the resource file.
        /// </summary>
        /// <param name="forceSortFileContent">if set to <c>true</c> to force sorting the file content.</param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save(bool forceSortFileContent)
        {
            var configuration = _resourceManager.Configuration;

            if (forceSortFileContent || configuration.SortFileContentOnSave)
            {
                var nodes = _documentRoot.Elements(@"data").ToArray();

                foreach (var item in nodes)
                {
                    Contract.Assume(item != null);
                    item.Remove();
                }

                var stringComparison = configuration.ResXSortingComparison;
                var comparer = new DelegateComparer<string>((left, right) => string.Compare(left, right, stringComparison));

                foreach (var item in nodes.OrderBy(node => node.TryGetAttribute("name").TrimStart('>'), comparer))
                {
                    _documentRoot.Add(item);
                }
            }

            const string declaration = @"<?xml version=""1.0"" encoding=""utf-8""?>";

            _file.Content = declaration + Environment.NewLine + _document;

            HasChanges = false;
        }

        internal string GetComment(string key)
        {
            Contract.Requires(key != null);

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
            Contract.Requires(key != null);

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
            Contract.Requires(oldKey != null);
            Contract.Requires(!string.IsNullOrEmpty(newKey));

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
            Contract.Requires(key != null);

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
            Contract.Requires(key != null);

            return _nodes.ContainsKey(key);
        }

        private static void MakeKeysUnique(ICollection<Node> elements)
        {
            Contract.Requires(elements != null);

            var itemsWithDuplicateKeys = elements.GroupBy(item => item.Key)
                .Where(group => group.Count() > 1);

            foreach (var duplicates in itemsWithDuplicateKeys)
            {
                Contract.Assume(duplicates != null);
                var index = 1;

                duplicates.Skip(1).ForEach(item => item.Key = GenerateUniqueKey(elements, item, ref index));
            }
        }

        private static string GenerateUniqueKey(ICollection<Node> elements, Node item, ref int index)
        {
            Contract.Requires(elements != null);
            Contract.Requires(item != null);

            var key = item.Key;
            string newKey;

            do
            {
                newKey = string.Format(CultureInfo.InvariantCulture, "{0}_Duplicate[{1}]", key, index);
                index += 1;
            }
            while (elements.Any(element => element.Key.Equals(newKey, StringComparison.OrdinalIgnoreCase)));

            return newKey;
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
                Contract.Requires(owner != null);
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
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileNameAttributeMissingError, _owner.ProjectFile.FilePath));
                }

                return nameAttribute;
            }



            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_element != null);
                Contract.Invariant(_owner != null);
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
            Contract.Invariant(_document != null);
            Contract.Invariant(_documentRoot != null);
            Contract.Invariant(_file != null);
            Contract.Invariant(_nodes != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_cultureKey != null);
        }
    }
}
