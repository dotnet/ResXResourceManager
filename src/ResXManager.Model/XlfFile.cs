namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using static XlfNames;

    public class XlfFile
    {
        private readonly XElement _fileElement;

        internal XlfFile(XlfDocument document, XElement fileElement)
        {
            Document = document;
            _fileElement = fileElement;
        }

        public XlfDocument Document { get; }

        public string? Original
        {
            get => _fileElement.Attribute(OriginalAttribute)?.Value;
            set => _fileElement.SetAttributeValue(OriginalAttribute, value);
        }

        public string? SourceLanguage
        {
            get => _fileElement.Attribute(SourceLanguageAttribute)?.Value;
            set => _fileElement.SetAttributeValue(SourceLanguageAttribute, value);
        }

        public string? TargetLanguage
        {
            get => _fileElement.Attribute(TargetLanguageAttribute)?.Value;
            set => _fileElement.SetAttributeValue(TargetLanguageAttribute, value);
        }

        public IEnumerable<ResourceNode> ResourceNodes
        {
            get
            {
                var bodyElement = _fileElement.Element(BodyElement);

                foreach (var transUnitElement in bodyElement.Descendants(TransUnitElement).ToList())
                {
                    var id = transUnitElement.GetId();
                    var state = transUnitElement.GetTargetState();
                    var target = (state != NewState) ? transUnitElement.GetTargetValue() : null;
                    var note = transUnitElement.GetNoteValue();

                    yield return new ResourceNode(id, target, note);
                }
            }
        }

        public bool Update(ResourceLanguage neutralLanguage, ResourceLanguage targetLanguage)
        {
            var changed = false;
            var neutralNodesById = neutralLanguage.GetNodes().ToDictionary(node => node.Key);
            var targetNodesById = targetLanguage.GetNodes().ToDictionary(node => node.Key);

            var bodyElement = _fileElement
                .Element(BodyElement);

            var transUnitsById = bodyElement
                .Descendants(TransUnitElement)
                .GroupBy(element => element.GetId())
                .Where(group => !string.IsNullOrEmpty(group.Key) && group.Any())
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var transUnit in transUnitsById)
            {
                var id = transUnit.Key;
                var transUnitElement = transUnit.Value;

                if (!neutralNodesById.TryGetValue(id, out var neutralNode))
                {
                    transUnit.Value.Remove();
                    changed = true;
                    continue;
                }

                targetNodesById.TryGetValue(id, out var targetNode);

                var state = transUnit.Value.GetTargetState();
                var source = transUnitElement.GetSourceValue();
                var target = transUnitElement.GetTargetValue();
                var note = transUnitElement.GetNoteValue();

                if (source != neutralNode.Text || (neutralNode.Comment != null && note != neutralNode.Comment) || target != targetNode?.Text)
                {
                    transUnitElement.SetSourceValue(neutralNode.Text ?? string.Empty);
                    if (neutralNode.Comment != null)
                    {
                        transUnitElement.SetNoteValue(neutralNode.Comment);
                    }

                    transUnitElement.SetTargetValue(targetNode?.Text ?? string.Empty);

                    if (state == TranslatedState)
                    {
                        transUnitElement.SetTargetState(NeedsReviewState);
                    }

                    changed = true;
                }

                neutralNodesById.Remove(id);
            }

            foreach (var neutralNode in neutralNodesById.Values)
            {
                var newTransUnit =
                    new XElement(TransUnitElement,
                        new XAttribute(IdAttribute, neutralNode.Key),
                        new XElement(SourceElement, neutralNode.Text),
                        new XElement(TargetElement, new XAttribute("state", "new"), string.Empty),
                        new XElement(NoteElement, string.IsNullOrEmpty(neutralNode.Comment) ? null : neutralNode.Comment));

                var nextElement = bodyElement
                    .Descendants(TransUnitElement)
                    .FirstOrDefault(element => StringComparer.Ordinal.Compare(neutralNode.Key, element.GetId()) < 0);

                if (nextElement != null)
                {
                    nextElement.AddBeforeSelf(newTransUnit);
                }
                else
                {
                    var lastElement = bodyElement
                        .Descendants(TransUnitElement)
                        .LastOrDefault();

                    if (lastElement != null)
                    {
                        lastElement.AddAfterSelf(newTransUnit);
                    }
                    else
                    {
                        bodyElement.Add(newTransUnit);
                    }
                }

                changed = true;
            }

            if (changed)
            {
                Document.Save();
            }
            return changed;
        }

        internal static XElement CreateEmpty(string original, string sourceLanguage, string targetLanguage)
        {
            return new XElement(FileElement,
                new XAttribute(DataTypeAttribute, "xml"),
                new XAttribute(SourceLanguageAttribute, sourceLanguage),
                new XAttribute(TargetLanguageAttribute, targetLanguage),
                new XAttribute(OriginalAttribute, original), // placeholder will be replaced on first update
                new XElement(BodyElement));
        }
    }
}
