namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using TomsToolbox.Essentials;

    using static XlfNames;

    public class XlfDocument : XmlFile
    {
        private XDocument _document;
        private List<XlfFile> _files;

        public XlfDocument(string filePath)
            : base(filePath)
        {
            _document = TryLoadFromFile() ?? CreateEmpty();

            if (_document.Root.GetAttribute("version") != "1.2")
                throw new InvalidOperationException("Only XLIFF version 1.2 is supported: " + filePath);

            _files = _document.Root.Elements(FileElement)
                .Select(file => new XlfFile(this, file))
                .ToList();
        }

        private static XDocument CreateEmpty()
        {
            return new XDocument(
                new XElement(Xliff,
                    new XAttribute("version", "1.2"),
                    new XAttribute("xmlns", XliffNS.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xsi", XsiNS.NamespaceName),
                    new XAttribute(XsiNS + "schemaLocation", $"{XliffNS.NamespaceName} xliff-core-1.2-transitional.xsd")));
        }

        public ICollection<XlfFile> Files => _files.AsReadOnly();

        public XlfFile AddFile(string original, string sourceLanguage, string targetLanguage)
        {
            var fileElement = new XElement(FileElement,
                new XAttribute(DataTypeAttribute, "xml"),
                new XAttribute(SourceLanguageAttribute, sourceLanguage),
                new XAttribute(TargetLanguageAttribute, targetLanguage),
                new XAttribute(OriginalAttribute, original),
                new XElement(BodyElement));

            _document.Root.Add(fileElement);

            var file = new XlfFile(this, fileElement);

            _files.Add(file);

            return file;
        }

        public void Save()
        {
            SaveToFile(_document);
        }

        public XlfDocument Reload()
        {
            _document = TryLoadFromFile() ?? CreateEmpty();

            _files = _document.Root.Elements(FileElement).Select(file => new XlfFile(this, file)).ToList();

            return this;
        }
    }
}
