namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using TomsToolbox.Essentials;

    using static XlfNames;

    public class XlfResourceNode : ResourceNode
    {
        public XlfResourceNode(string key, string? text, string? comment, TranslationState translationState)
            : base(key, text, comment)
        {
            TranslationState = translationState;
        }

        public TranslationState TranslationState { get; }
    }

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

        public IEnumerable<XlfResourceNode> ResourceNodes
        {
            get
            {
                var bodyElement = _fileElement.Element(BodyElement);

                foreach (var transUnitElement in bodyElement.Descendants(TransUnitElement).ToList())
                {
                    var id = transUnitElement.GetId();
                    if (id.IsNullOrEmpty())
                        continue;

                    var target = transUnitElement.GetTargetValue();
                    var specificComment = transUnitElement.GetNoteValue(FromResxSpecific);
                    var state = transUnitElement.GetTargetState();

                    yield return new XlfResourceNode(id, target, specificComment, state);
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
                if (id.IsNullOrEmpty())
                    continue;

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
                var target = transUnitElement.GetTargetValue() ?? string.Empty;
                var neutralNote = transUnitElement.GetNoteValue(FromResx) ?? string.Empty;
                var specificNote = transUnitElement.GetNoteValue(FromResxSpecific) ?? string.Empty;

                var neutralText = neutralNode.Text ?? string.Empty;
                var neutralComment = neutralNode.Comment ?? string.Empty;
                var specificComment = targetNode?.Comment ?? string.Empty;
                var targetText = targetNode?.Text ?? string.Empty;

                ResourceTableEntry.ExtractCommentTokens(ref specificComment, out var translationState, out _);

                if (translationState.HasValue)
                {
                    if (state != translationState.Value)
                    {
                        transUnitElement.SetTargetState(translationState.Value);
                    }

                    changed = true;
                }

                if (source != neutralText)
                {
                    transUnitElement.SetSourceValue(neutralText);
                    if (state == TranslationState.Approved)
                    {
                        transUnitElement.SetTargetState(NeedsReviewState);
                    }

                    changed = true;
                }

                if (neutralComment != neutralNote)
                {
                    // TODO: change translation state if comment changes?
                    transUnitElement.SetNoteValue(FromResx, neutralComment);
                    changed = true;
                }

                if (specificComment != specificNote)
                {
                    // TODO: change translation state if comment changes?
                    transUnitElement.SetNoteValue(FromResxSpecific, specificComment);
                    changed = true;
                }

                if (target != targetText)
                {
                    transUnitElement.SetTargetValue(targetText);
                    if (state == TranslationState.Approved)
                    {
                        transUnitElement.SetTargetState(NeedsReviewState);
                    }

                    changed = true;
                }

                translationState = GetEffectiveXlfTranslationState(translationState, targetText);

                if (state != translationState)
                {
                    transUnitElement.SetTargetState(translationState.Value);
                    changed = true;
                }

                neutralNodesById.Remove(id);
            }

            foreach (var neutralNode in neutralNodesById.Values)
            {
                targetNodesById.TryGetValue(neutralNode.Key, out var targetNode);
                var neutralComment = neutralNode.Comment ?? string.Empty;
                var specificComment = targetNode?.Comment ?? string.Empty;
                var targetText = targetNode?.Text ?? string.Empty;

                ResourceTableEntry.ExtractCommentTokens(ref specificComment, out var translationState, out _);

                var newTransUnit =
                    new XElement(TransUnitElement,
                        new XAttribute(IdAttribute, neutralNode.Key),
                        new XAttribute(TranslateAttribute, "yes"),
                        new XAttribute(XNamespace.Xml.GetName(@"space"), "preserve"),
                        new XElement(SourceElement, neutralNode.Text),
                        new XElement(TargetElement, new XAttribute(StateAttribute, NewState), targetText));

                if (translationState.HasValue)
                {
                    newTransUnit.SetTargetState(translationState.Value);
                }

                if (!string.IsNullOrEmpty(neutralComment))
                {
                    newTransUnit.SetNoteValue(FromResx, neutralComment);
                }

                if (!string.IsNullOrEmpty(specificComment))
                {
                    newTransUnit.SetNoteValue(FromResxSpecific, specificComment);
                }

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
                        var groupElement = bodyElement.Element(GroupElement);
                        if (groupElement == null)
                        {
                            groupElement = new XElement(GroupElement, new XAttribute(IdAttribute, Original), new XAttribute(DataTypeAttribute, "resx"));
                            bodyElement.Add(groupElement);
                        }
                        groupElement.Add(newTransUnit);
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

        public static TranslationState GetEffectiveXlfTranslationState(TranslationState? state, string? value)
        {
            if (state == null)
            {
                return string.IsNullOrEmpty(value) ? TranslationState.New : TranslationState.Approved;
            }

            return state.Value;
        }
    }
}
