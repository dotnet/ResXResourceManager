namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using static XlfNames;

    public class XlfDocument : XmlFile
    {
        private XDocument _document;

        public XlfDocument(string filePath)
            : base(filePath)
        {
            _document = LoadFromFile();

            Files = _document.Root.Elements(FileElement).Select(file => new XlfFile(this, file)).ToList();
        }

        private static XDocument CreateEmpty()
        {
            return new XDocument(
                new XElement(Xliff,
                    new XAttribute("xmlns", XliffNS.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xsi", XsiNS.NamespaceName),
                    new XAttribute("version", "1.2"),
                    new XAttribute(XsiNS + "schemaLocation", $"{XliffNS.NamespaceName} xliff-core-1.2-transitional.xsd")));
        }

        public ICollection<XlfFile> Files { get; private set; }

        public void Save()
        {
            SaveToFile(_document);
        }

        public XlfDocument Reload()
        {
            _document = LoadFromFile();

            Files = _document.Root.Elements(FileElement).Select(file => new XlfFile(this, file)).ToList();

            return this;
        }
    }
}
