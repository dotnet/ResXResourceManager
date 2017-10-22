namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using Throttle;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Represents a set of localized resources.
    /// </summary>
    [Localizable(false)]
    public class ResourceLanguage
    {
        [NotNull]
        private const string Quote = "\"";
        [NotNull]
        private const string WinFormsMemberNamePrefix = @">>";
        [NotNull]
        private static readonly XName _spaceAttributeName = XNamespace.Xml.GetName(@"space");
        [NotNull]
        private static readonly XName _typeAttributeName = XNamespace.None.GetName(@"type");
        [NotNull]
        private static readonly XName _mimetypeAttributeName = XNamespace.None.GetName(@"mimetype");
        [NotNull]
        private static readonly XName _nameAttributeName = XNamespace.None.GetName(@"name");

        [NotNull]
        private readonly XDocument _document;

        [NotNull]
        // ReSharper disable once AssignNullToNotNullAttribute
        private XElement _documentRoot => _document.Root;

        [NotNull]
        private readonly ProjectFile _file;
        [NotNull]
        private IDictionary<string, Node> _nodes = new Dictionary<string, Node>();
        [NotNull]
        private readonly CultureKey _cultureKey;

        [NotNull]
        private readonly XName _dataNodeName;
        [NotNull]
        private readonly XName _valueNodeName;
        [NotNull]
        private readonly XName _commentNodeName;
        [NotNull]
        private readonly ResourceEntity _container;
        [NotNull]
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLanguage" /> class.
        /// </summary>
        /// <param name="container">The containing resource entity.</param>
        /// <param name="cultureKey">The culture key.</param>
        /// <param name="file">The .resx file having all the localization.</param>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        internal ResourceLanguage([NotNull] ResourceEntity container, [NotNull] CultureKey cultureKey, [NotNull] ProjectFile file)
        {
            Contract.Requires(container != null);
            Contract.Requires(cultureKey != null);
            Contract.Requires(file != null);

            _container = container;
            _cultureKey = cultureKey;
            _file = file;
            _configuration = container.Container.Configuration;

            try
            {
                _document = file.Load();
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath), ex);
            }

            if (_documentRoot == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath));

            var defaultNamespace = _documentRoot.GetDefaultNamespace();

            _dataNodeName = defaultNamespace.GetName(@"data");
            _valueNodeName = defaultNamespace.GetName(@"value");
            _commentNodeName = defaultNamespace.GetName(@"comment");

            UpdateNodes();
        }

        private void UpdateNodes()
        {
            var data = _documentRoot.Elements(_dataNodeName);

            var elements = data
                .Where(IsStringType)
                .Select(item => new Node(this, item))
                .Where(item => !item.Key.StartsWith(WinFormsMemberNamePrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (_configuration.DuplicateKeyHandling == DuplicateKeyHandling.Rename)
            {
                MakeKeysValid(elements);
            }
            else
            {
                if (elements.Any(item => string.IsNullOrEmpty(item.Key)))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.EmptyKeysError, _file.FilePath));
            }

            try
            {
                _nodes = elements.ToDictionary(item => item.Key);
            }
            catch (ArgumentException ex)
            {
                var duplicateKeys = string.Join(@", ", elements.GroupBy(item => item.Key).Where(group => group.Count() > 1).Select(group => Quote + group.Key + Quote));
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateKeyError, _file.FilePath, duplicateKeys), ex);
            }
        }

        /// <summary>
        /// Gets the culture of this language.
        /// </summary>
        [CanBeNull]
        public CultureInfo Culture => _cultureKey.Culture;

        /// <summary>
        /// Gets the display name of this language.
        /// </summary>
        [NotNull]
        public string DisplayName => ToString();

        /// <summary>
        /// Gets all the resource keys defined in this language.
        /// </summary>
        [NotNull, ItemNotNull, ContractVerification(false)]
        public IEnumerable<string> ResourceKeys
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<string>>(), item => item != null));

                return _nodes.Keys;
            }
        }

        public bool HasChanges => _file.HasChanges;

        public bool IsSaving { get; private set; }

        [NotNull]
        public string FileName
        {
            get
            {
                Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

                return _file.FilePath;
            }
        }

        [NotNull]
        public ProjectFile ProjectFile
        {
            get
            {
                Contract.Ensures(Contract.Result<ProjectFile>() != null);

                return _file;
            }
        }

        public bool IsNeutralLanguage => Container.Languages.FirstOrDefault() == this;

        [NotNull]
        public CultureKey CultureKey
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureKey>() != null);

                return _cultureKey;
            }
        }

        [NotNull]
        public ResourceEntity Container
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceEntity>() != null);

                return _container;
            }
        }

        private static bool IsStringType([NotNull] XElement entry)
        {
            Contract.Requires(entry != null);

            var typeAttribute = entry.Attribute(_typeAttributeName);

            if (typeAttribute != null)
            {
                return string.IsNullOrEmpty(typeAttribute.Value) || typeAttribute.Value.StartsWith(typeof(string).Name, StringComparison.OrdinalIgnoreCase);
            }

            var mimeTypeAttribute = entry.Attribute(_mimetypeAttributeName);

            return mimeTypeAttribute == null;
        }

        internal string GetValue([NotNull] string key)
        {
            Contract.Requires(key != null);

            return !_nodes.TryGetValue(key, out Node node) ? null : node?.Text;
        }

        internal bool SetValue([NotNull] string key, string value)
        {
            Contract.Requires(key != null);

            return GetValue(key) == value || SetNodeData(key, node => node.Text = value);
        }

        public void ForceValue([NotNull] string key, string value)
        {
            Contract.Requires(key != null);

            SetNodeData(key, node => node.Text = value);
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void OnChanged()
        {
            _file.Changed(_document, _configuration.SaveFilesImmediatelyUponChange);

            Container.Container.OnLanguageChanged(this);
        }

        internal bool CanEdit()
        {
            return Container.CanEdit(CultureKey);
        }

        /// <summary>
        /// Saves this instance to the resource file.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            try
            {
                IsSaving = true;

                _file.Save(_document);

                Container.Container.OnProjectFileSaved(this, _file);
            }
            finally
            {
                IsSaving = false;
            }
        }

        public void SortNodes(StringComparison stringComparison)
        {
            if (!SortDocument(stringComparison))
                return;

            UpdateNodes();
            Container.OnItemOrderChanged(this);

            _file.Changed(_document, true);

            Save();
        }

        private bool SortDocument(StringComparison stringComparison)
        {
            return SortAndAdd(stringComparison, null);
        }

        private bool SortAndAdd(StringComparison stringComparison, [CanBeNull] XElement newNode)
        {
            var comparer = new DelegateComparer<string>((left, right) => string.Compare(left, right, stringComparison));
            string GetName(XElement node) => node.Attribute(_nameAttributeName)?.Value.TrimStart('>') ?? string.Empty;

            var nodes = _documentRoot
                .Elements(_dataNodeName)
                .ToArray();

            var sortedNodes = nodes
                .OrderBy(node => GetName(node), comparer)
                .ToArray();

            var hasContentChanged = SortNodes(nodes, sortedNodes);

            if (newNode == null)
                return hasContentChanged;

            var newNodeName = GetName(newNode);
            var nextNode = sortedNodes.FirstOrDefault(node => comparer.Compare(GetName(node), newNodeName) > 0);

            if (nextNode != null)
            {
                nextNode.AddBeforeSelf(newNode);
            }
            else
            {
                _documentRoot.Add(newNode);
            }

            return true;
        }

        private bool SortNodes([NotNull, ItemNotNull] XElement[] nodes, [NotNull, ItemNotNull] XElement[] sortedNodes)
        {
            Contract.Requires(nodes != null);
            Contract.Requires(sortedNodes != null);

            if (nodes.SequenceEqual(sortedNodes))
                return false;

            foreach (var item in nodes)
            {
                Contract.Assume(item != null);
                item.Remove();
            }

            foreach (var item in sortedNodes)
            {
                _documentRoot.Add(item);
            }

            return true;
        }

        [CanBeNull]
        internal string GetComment([NotNull] string key)
        {
            Contract.Requires(key != null);

            if (!_nodes.TryGetValue(key, out Node node) || (node == null))
                return null;

            return node.Comment;
        }

        internal bool SetComment([NotNull] string key, string value)
        {
            Contract.Requires(key != null);

            if (GetComment(key) == value)
                return true;

            return SetNodeData(key, node => node.Comment = value);
        }

        private bool SetNodeData([NotNull] string key, [NotNull] Action<Node> updateCallback)
        {
            Contract.Requires(key != null);
            Contract.Requires(updateCallback != null);

            if (!CanEdit())
                return false;

            try
            {
                if (!_nodes.TryGetValue(key, out Node node) || (node == null))
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

                OnChanged();
                return true;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _file.FilePath, ex.Message);
                throw new IOException(message, ex);
            }
        }

        [NotNull]
        private Node CreateNode([NotNull] string key)
        {
            Contract.Requires(key != null);
            Contract.Ensures(Contract.Result<Node>() != null);

            var content = new XElement(_valueNodeName);
            content.Add(new XText(string.Empty));

            var entry = new XElement(_dataNodeName, new XAttribute(_nameAttributeName, key), new XAttribute(_spaceAttributeName, @"preserve"));
            entry.Add(content);

            var fileContentSorting = _configuration.EffectiveResXSortingComparison;

            if (fileContentSorting.HasValue)
            {
                SortAndAdd(fileContentSorting.Value, entry);
            }
            else
            {
                _documentRoot.Add(entry);
            }

            UpdateNodes();

            Dispatcher.CurrentDispatcher.BeginInvoke(() => Container.OnItemOrderChanged(this));

            return _nodes[key];
        }

        internal bool RenameKey([NotNull] string oldKey, [NotNull] string newKey)
        {
            Contract.Requires(oldKey != null);
            Contract.Requires(!string.IsNullOrEmpty(newKey));

            if (!CanEdit())
                return false;

            if (!_nodes.TryGetValue(oldKey, out Node node) || (node == null))
                return false;

            if (_nodes.ContainsKey(newKey))
                return false;

            _nodes.Remove(oldKey);
            node.Key = newKey;
            _nodes.Add(newKey, node);

            OnChanged();
            return true;
        }

        internal bool RemoveKey([NotNull] string key)
        {
            Contract.Requires(key != null);

            if (!CanEdit())
                return false;

            try
            {
                if (!_nodes.TryGetValue(key, out Node node) || (node == null))
                {
                    return false;
                }

                node.Element.Remove();
                _nodes.Remove(key);

                OnChanged();
                return true;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _file.FilePath, ex.Message);
                throw new IOException(message, ex);
            }
        }

        internal bool KeyExists([NotNull] string key)
        {
            Contract.Requires(key != null);

            return _nodes.ContainsKey(key);
        }

        internal void MoveNode([NotNull] ResourceTableEntry resourceTableEntry, [NotNull] IEnumerable<ResourceTableEntry> previousEntries)
        {
            Contract.Requires(resourceTableEntry != null);
            Contract.Requires(previousEntries != null);

            if (!CanEdit())
                return;

            var node = _nodes.GetValueOrDefault(resourceTableEntry.Key);

            if (node == null)
                return;

            var prevousNode = previousEntries
                .Select(entry => _nodes.GetValueOrDefault(entry.Key))
                .FirstOrDefault(item => item != null);

            if (prevousNode == null)
                return;

            var element = node.Element;
            element.Remove();
            prevousNode.Element.AddAfterSelf(element);

            OnChanged();
        }

        [ContractVerification(false)]
        internal bool IsContentEqual([NotNull] ResourceLanguage other)
        {
            Contract.Requires(other != null);

            return _document.ToString(SaveOptions.DisableFormatting) == other._document.ToString(SaveOptions.DisableFormatting);
        }

        private static void MakeKeysValid([NotNull] ICollection<Node> elements)
        {
            Contract.Requires(elements != null);

            RenameEmptyKeys(elements);

            RenameDuplicates(elements);
        }

        private static void RenameDuplicates([NotNull, ItemNotNull] ICollection<Node> elements)
        {
            Contract.Requires(elements != null);

            var itemsWithDuplicateKeys = elements.GroupBy(item => item.Key)
                .Where(group => group.Count() > 1);

            foreach (var duplicates in itemsWithDuplicateKeys)
            {
                Contract.Assume(duplicates != null);
                var index = 1;

                duplicates.Skip(1).ForEach(item => item.Key = GenerateUniqueKey(elements, item, "Duplicate", ref index));
            }
        }

        private static void RenameEmptyKeys([NotNull, ItemNotNull] ICollection<Node> elements)
        {
            Contract.Requires(elements != null);

            var itemsWithEmptyKeys = elements.Where(item => string.IsNullOrEmpty(item.Key));

            var index = 1;

            itemsWithEmptyKeys.ForEach(item => item.Key = GenerateUniqueKey(elements, item, "Empty", ref index));
        }

        [NotNull]
        private static string GenerateUniqueKey([NotNull] ICollection<Node> elements, [NotNull] Node item, string text, ref int index)
        {
            Contract.Requires(elements != null);
            Contract.Requires(item != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var key = item.Key;
            string newKey;

            do
            {
                newKey = string.Format(CultureInfo.InvariantCulture, "{0}_{1}[{2}]", key, text, index);
                index += 1;
            }
            while (elements.Any(element => element.Key.Equals(newKey, StringComparison.OrdinalIgnoreCase)));

            return newKey;
        }

        public override string ToString()
        {
            return Culture?.DisplayName ?? Resources.Neutral;
        }

        private class Node
        {
            [NotNull]
            private readonly ResourceLanguage _owner;
            [NotNull]
            private readonly XElement _element;
            private string _text;
            private string _comment;

            public Node([NotNull] ResourceLanguage owner, [NotNull] XElement element)
            {
                Contract.Requires(owner != null);
                Contract.Requires(element != null);
                Contract.Requires(owner._commentNodeName != null);

                _element = element;
                _owner = owner;
            }

            [NotNull]
            public XElement Element
            {
                get
                {
                    Contract.Ensures(Contract.Result<XElement>() != null);

                    return _element;
                }
            }

            [NotNull]
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

                    var valueElement = entry.Element(_owner._valueNodeName);
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

                    var valueElement = entry.Element(_owner._commentNodeName);

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
                            valueElement = new XElement(_owner._commentNodeName);
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

                var valueElement = entry.Element(_owner._valueNodeName);
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

                var valueElement = entry.Element(_owner._commentNodeName);
                if (valueElement == null)
                    return string.Empty;

                var textNode = valueElement.FirstNode as XText;

                return textNode == null ? string.Empty : textNode.Value;
            }

            [NotNull]
            private XAttribute GetNameAttribute([NotNull] XElement entry)
            {
                Contract.Requires(entry != null);
                Contract.Ensures(Contract.Result<XAttribute>() != null);

                var nameAttribute = entry.Attribute(_nameAttributeName);
                if (nameAttribute == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileNameAttributeMissingError, _owner.ProjectFile.FilePath));
                }

                return nameAttribute;
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_element != null);
                Contract.Invariant(_owner != null);
                Contract.Invariant(_owner._commentNodeName != null);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_document != null);
            Contract.Invariant(_documentRoot != null);
            Contract.Invariant(_file != null);
            Contract.Invariant(_nodes != null);
            Contract.Invariant(_cultureKey != null);
            Contract.Invariant(_dataNodeName != null);
            Contract.Invariant(_valueNodeName != null);
            Contract.Invariant(_commentNodeName != null);
            Contract.Invariant(_container != null);
        }
    }
}