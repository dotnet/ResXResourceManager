namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Represents a set of localized resources.
    /// </summary>
    [Localizable(false)]
    public class ResourceLanguage
    {
        private const string Quote = "\"";
        private const string WinFormsMemberNamePrefix = @">>";
        private static readonly XName _spaceAttributeName = XNamespace.Xml.GetName(@"space");
        private static readonly XName _typeAttributeName = XNamespace.None.GetName(@"type");
        private static readonly XName _mimetypeAttributeName = XNamespace.None.GetName(@"mimetype");
        private static readonly XName _nameAttributeName = XNamespace.None.GetName(@"name");

        private readonly XDocument _document;

        // ReSharper disable once AssignNullToNotNullAttribute
        private XElement DocumentRoot => _document.Root;

        private IDictionary<string, Node> _nodes = new Dictionary<string, Node>();

        private readonly XName _dataNodeName;
        private readonly XName _valueNodeName;
        private readonly XName _commentNodeName;

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLanguage" /> class.
        /// </summary>
        /// <param name="container">The containing resource entity.</param>
        /// <param name="cultureKey">The culture key.</param>
        /// <param name="file">The .resx file having all the localization.</param>
        /// <param name="duplicateKeyHandling">The duplicate key handling.</param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        /// <exception cref="InvalidOperationException"></exception>
        internal ResourceLanguage(ResourceEntity container, CultureKey cultureKey, ProjectFile file, DuplicateKeyHandling duplicateKeyHandling)
        {
            Container = container;
            CultureKey = cultureKey;
            ProjectFile = file;
            _configuration = container.Container.Configuration;

            try
            {
                _document = file.Load();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath), ex);
            }

            if (DocumentRoot == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileError, file.FilePath));

            var defaultNamespace = DocumentRoot.GetDefaultNamespace();

            _dataNodeName = defaultNamespace.GetName(@"data");
            _valueNodeName = defaultNamespace.GetName(@"value");
            _commentNodeName = defaultNamespace.GetName(@"comment");

            UpdateNodes(duplicateKeyHandling);
        }

        private void UpdateNodes(DuplicateKeyHandling duplicateKeyHandling)
        {
            var data = DocumentRoot.Elements(_dataNodeName);

            var elements = data
                .Where(IsStringType)
                .Select(item => new Node(this, item))
                .Where(item => !item.Key.StartsWith(WinFormsMemberNamePrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (duplicateKeyHandling == DuplicateKeyHandling.Rename)
            {
                MakeKeysValid(elements);
            }
            else
            {
                if (elements.Any(item => string.IsNullOrEmpty(item.Key)))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.EmptyKeysError, ProjectFile.FilePath));
            }

            try
            {
                _nodes = elements.ToDictionary(item => item.Key);
            }
            catch (ArgumentException ex)
            {
                var duplicateKeys = string.Join(@", ", elements.GroupBy(item => item.Key).Where(group => group.Count() > 1).Select(group => Quote + group.Key + Quote));
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateKeyError, ProjectFile.FilePath, duplicateKeys), ex);
            }
        }

        /// <summary>
        /// Gets the culture of this language.
        /// </summary>
        public CultureInfo? Culture => CultureKey.Culture;

        /// <summary>
        /// Gets the display name of this language.
        /// </summary>
        public string DisplayName => ToString();

        /// <summary>
        /// Gets all the resource keys defined in this language.
        /// </summary>
        public IEnumerable<string> ResourceKeys => _nodes.Keys;

        public bool HasChanges => ProjectFile.HasChanges;

        public bool IsSaving { get; private set; }

        public string FileName => ProjectFile.FilePath;

        public ProjectFile ProjectFile { get; }

        public bool IsNeutralLanguage => Container.Languages.FirstOrDefault() == this;

        public CultureKey CultureKey { get; }

        public ResourceEntity Container { get; }

        private static bool IsStringType(XElement entry)
        {
            var typeAttribute = entry.Attribute(_typeAttributeName);

            if (typeAttribute != null)
            {
                return string.IsNullOrEmpty(typeAttribute.Value) || typeAttribute.Value.StartsWith(nameof(String), StringComparison.OrdinalIgnoreCase);
            }

            var mimeTypeAttribute = entry.Attribute(_mimetypeAttributeName);

            return mimeTypeAttribute == null;
        }

        public ICollection<ResourceNode> GetNodes()
        {
            return _nodes.Values
                .Select(node => new ResourceNode(node.Key, node.Text, node.Comment))
                .ToList()
                .AsReadOnly();
        }

        internal string? GetValue(string key)
        {
            return !_nodes.TryGetValue(key, out var node) ? null : node?.Text;
        }

        internal bool SetValue(string key, string? value)
        {
            if ((GetValue(key) ?? string.Empty) == (value ?? string.Empty))
                return false;

            SetNodeData(key, node => node.Text = value);
            return true;
        }

        public void ForceValue(string key, string? value)
        {
            SetNodeData(key, node => node.Text = value);
        }

        private void OnChanged()
        {
            ProjectFile.Changed(_document);

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

                ProjectFile.Save(_document);

                Container.Container.OnProjectFileSaved(this, ProjectFile);
            }
            finally
            {
                IsSaving = false;
            }
        }

        public void SortNodes(StringComparison stringComparison, DuplicateKeyHandling duplicateKeyHandling)
        {
            if (!SortDocument(stringComparison))
                return;

            UpdateNodes(duplicateKeyHandling);
            Container.OnItemOrderChanged(this);

            ProjectFile.Changed(_document);

            Save();
        }

        private bool SortDocument(StringComparison stringComparison)
        {
            return SortAndAdd(stringComparison, null);
        }

        private bool SortAndAdd(StringComparison stringComparison, XElement? newNode)
        {
            var comparer = new DelegateComparer<string>((left, right) => string.Compare(left, right, stringComparison));

            static string GetName(XElement node)
            {
                return node.Attribute(_nameAttributeName)?.Value.TrimStart('>') ?? string.Empty;
            }

            var nodes = DocumentRoot
                .Elements(_dataNodeName)
                .ToArray();

            var sortedNodes = nodes
                .OrderBy(GetName, comparer)
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
                DocumentRoot.Add(newNode);
            }

            return true;
        }

        private bool SortNodes(XElement[] nodes, XElement[] sortedNodes)
        {
            if (nodes.SequenceEqual(sortedNodes))
                return false;

            foreach (var item in nodes)
            {
                item.Remove();
            }

            foreach (var item in sortedNodes)
            {
                DocumentRoot.Add(item);
            }

            return true;
        }

        internal string? GetComment(string key)
        {
            if (!_nodes.TryGetValue(key, out var node) || (node == null))
                return null;

            return node.Comment;
        }

        internal bool SetComment(string key, string? value)
        {
            if ((GetComment(key) ?? string.Empty) == (value ?? string.Empty))
                return false;

            SetNodeData(key, node => node.Comment = value);

            return true;
        }

        private void SetNodeData(string key, Action<Node> updateCallback)
        {
            if (!CanEdit())
                throw new InvalidOperationException("Language file can't be edited right now: " + FileName);

            try
            {
                if (!_nodes.TryGetValue(key, out var node))
                {
                    node = CreateNode(key);
                }

                updateCallback(node);

                if (!IsNeutralLanguage)
                {
                    if (_configuration.RemoveEmptyEntries && string.IsNullOrEmpty(node.Text) && string.IsNullOrEmpty(node.Comment))
                    {
                        node.Element.Remove();
                        _nodes.Remove(key);
                    }
                }

                OnChanged();
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, ProjectFile.FilePath, ex.Message);
                throw new IOException(message, ex);
            }
        }

        private Node CreateNode(string key)
        {
            var content = new XElement(_valueNodeName);
            content.Add(new XText(string.Empty));

            var entry = new XElement(_dataNodeName, new XAttribute(_nameAttributeName, key), new XAttribute(_spaceAttributeName, @"preserve"));
            entry.Add(content, new XText("\n  "));

            var fileContentSorting = _configuration.EffectiveResXSortingComparison;

            if (fileContentSorting.HasValue)
            {
                SortAndAdd(fileContentSorting.Value, entry);
            }
            else
            {
                DocumentRoot.Add(entry);
            }

            UpdateNodes(_configuration.DuplicateKeyHandling);

            try
            {
                new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()).StartNew(() => Container.OnItemOrderChanged(this));
            }
            catch (InvalidOperationException)
            {
                // for scripting deferred notifications are not needed, so if the current thread does not support a synchronization context, just go without.
            }

            return _nodes[key];
        }

        internal bool RenameKey(string oldKey, string newKey)
        {
            if (!CanEdit())
                return false;

            if (!_nodes.TryGetValue(oldKey, out var node) || (node == null))
                return false;

            if (_nodes.ContainsKey(newKey))
                return false;

            _nodes.Remove(oldKey);
            node.Key = newKey;
            _nodes.Add(newKey, node);

            OnChanged();
            return true;
        }

        internal bool RemoveKey(string key)
        {
            if (!CanEdit())
                return false;

            try
            {
                if (!_nodes.TryGetValue(key, out var node) || (node == null))
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
                var message = string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, ProjectFile.FilePath, ex.Message);
                throw new IOException(message, ex);
            }
        }

        internal bool KeyExists(string key)
        {
            return _nodes.ContainsKey(key);
        }

        internal void MoveNode(ResourceTableEntry resourceTableEntry, IEnumerable<ResourceTableEntry> previousEntries)
        {
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

        internal bool IsContentEqual(ResourceLanguage other)
        {
            return _document.ToString(SaveOptions.DisableFormatting) == other._document.ToString(SaveOptions.DisableFormatting);
        }

        private static void MakeKeysValid(ICollection<Node> elements)
        {
            RenameEmptyKeys(elements);

            RenameDuplicates(elements);
        }

        private static void RenameDuplicates(ICollection<Node> elements)
        {
            var itemsWithDuplicateKeys = elements.GroupBy(item => item.Key)
                .Where(group => group.Count() > 1);

            foreach (var duplicates in itemsWithDuplicateKeys)
            {
                var index = 1;

                duplicates.Skip(1).ForEach(item => item.Key = GenerateUniqueKey(elements, item, "Duplicate", ref index));
            }
        }

        private static void RenameEmptyKeys(ICollection<Node> elements)
        {
            var itemsWithEmptyKeys = elements.Where(item => string.IsNullOrEmpty(item.Key));

            var index = 1;

            itemsWithEmptyKeys.ForEach(item => item.Key = GenerateUniqueKey(elements, item, "Empty", ref index));
        }

        private static string GenerateUniqueKey(ICollection<Node> elements, Node item, string? text, ref int index)
        {
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

        private sealed class Node
        {
            private readonly ResourceLanguage _owner;

            private string? _text;
            private string? _comment;
            private string _key;
            private readonly XElement _valueElement;
            private readonly XAttribute _nameAttribute;

            public Node(ResourceLanguage owner, XElement element)
            {
                Element = element;
                _owner = owner;
                _valueElement = element.Element(_owner._valueNodeName) ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileValueAttributeMissingError, owner.FileName));
                _nameAttribute = GetNameAttribute(element);
                _key = _nameAttribute.Value;
                _text = LoadText();
            }

            public XElement Element { get; }

            public string Key
            {
                get => _key;
                set => _key = _nameAttribute.Value = value;
            }

            public string? Text
            {
                get => _text;
                set
                {
                    _text = value ?? string.Empty;

                    if (_valueElement.FirstNode == null)
                    {
                        _valueElement.Add(value);
                    }
                    else
                    {
                        _valueElement.FirstNode.ReplaceWith(value);
                    }
                }
            }

            public string? Comment
            {
                get => _comment ??= LoadComment();
                set
                {
                    _comment = value ?? string.Empty;

                    var entry = Element;

                    var commentElement = entry.Element(_owner._commentNodeName);

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        commentElement?.Remove();
                    }
                    else
                    {
                        if (commentElement == null)
                        {
                            commentElement = new XElement(_owner._commentNodeName);
                            entry.Add(new XText("  "), commentElement, new XText("\n  "));
                        }

                        if (commentElement.FirstNode is XText textNode)
                        {
                            textNode.Value = value;
                        }
                        else
                        {
                            textNode = new XText(value);
                            commentElement.Add(textNode);
                        }
                    }
                }
            }

            private string? LoadText()
            {
                return _valueElement.FirstNode is XText textNode ? textNode.Value : string.Empty;
            }

            private string LoadComment()
            {
                var entry = Element;

                var commentElement = entry.Element(_owner._commentNodeName);

                return commentElement?.FirstNode is XText textNode ? textNode.Value : string.Empty;
            }

            private XAttribute GetNameAttribute(XElement entry)
            {
                var nameAttribute = entry.Attribute(_nameAttributeName);
                if (nameAttribute == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidResourceFileNameAttributeMissingError, _owner.ProjectFile.FilePath));
                }

                return nameAttribute;
            }
        }
    }
}