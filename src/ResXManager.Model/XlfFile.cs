namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using ResXManager.Infrastructure;

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

        public bool Update(ResourceEntity entity, CultureKey language)
        {
            var changed = false;

            var bodyElement = _fileElement
                .Element(BodyElement);

            var transUnitsById = bodyElement
                .Descendants(TransUnitElement)
                .GroupBy(element => element.GetId())
                .Where(group => !string.IsNullOrEmpty(group.Key) && group.Any())
                .ToDictionary(group => group.Key, group => group.First());

            var tableEntriesById = entity.Entries.ToDictionary(entry => entry.Key);

            foreach (var transUnit in transUnitsById)
            {
                var id = transUnit.Key;
                if (id.IsNullOrEmpty())
                    continue;

                var transUnitElement = transUnit.Value;

                if (!tableEntriesById.TryGetValue(id, out var tableEntry))
                {
                    transUnit.Value.Remove();
                    changed = true;
                    continue;
                }

                var state = transUnit.Value.GetTargetState();
                var source = transUnitElement.GetSourceValue();
                var target = transUnitElement.GetTargetValue() ?? string.Empty;
                var neutralNote = transUnitElement.GetNoteValue(FromResx) ?? string.Empty;
                var specificNote = transUnitElement.GetNoteValue(FromResxSpecific) ?? string.Empty;

                var neutralText = tableEntry.Values.GetValue(CultureKey.Neutral) ?? string.Empty;
                var neutralComment = tableEntry.GetCommentText(CultureKey.Neutral) ?? string.Empty;
                var targetText = tableEntry.Values.GetValue(language) ?? string.Empty;
                var specificCommentText = tableEntry.GetCommentText(language) ?? string.Empty;
                var translationState = tableEntry.TranslationState.GetValue(language);

                if (source != neutralText)
                {
                    transUnitElement.SetSourceValue(neutralText);
                    if (state == TranslationState.Approved)
                    {
                        transUnitElement.SetTargetState(NeedsReviewStates.First());
                    }

                    changed = true;
                }

                if (neutralComment != neutralNote)
                {
                    // TODO: change translation state if comment changes?
                    transUnitElement.SetNoteValue(FromResx, neutralComment);
                    changed = true;
                }

                if (specificCommentText != specificNote)
                {
                    // TODO: change translation state if comment changes?
                    transUnitElement.SetNoteValue(FromResxSpecific, specificCommentText);
                    changed = true;
                }

                if (target != targetText)
                {
                    transUnitElement.SetTargetValue(targetText);
                    if (state == TranslationState.Approved)
                    {
                        transUnitElement.SetTargetState(NeedsReviewStates.First());
                    }

                    changed = true;
                }

                if (state != translationState)
                {
                    transUnitElement.SetTargetState(translationState);
                    changed = true;
                }

                tableEntriesById.Remove(id);
            }

            foreach (var tableEntry in tableEntriesById.Values)
            {
                var neutralText = tableEntry.Values.GetValue(CultureKey.Neutral) ?? string.Empty;
                var neutralComment = tableEntry.GetCommentText(CultureKey.Neutral) ?? string.Empty;
                var targetText = tableEntry.Values.GetValue(language) ?? string.Empty;
                var specificCommentText = tableEntry.GetCommentText(language) ?? string.Empty;
                var translationState = tableEntry.TranslationState.GetValue(language);

                var newTransUnit =
                    new XElement(TransUnitElement,
                        new XAttribute(IdAttribute, tableEntry.Key),
                        new XAttribute(TranslateAttribute, "yes"),
                        new XAttribute(XNamespace.Xml.GetName(@"space"), "preserve"),
                        new XElement(SourceElement, neutralText),
                        new XElement(TargetElement, new XAttribute(StateAttribute, NewState), targetText));

                newTransUnit.SetTargetState(translationState);

                if (!string.IsNullOrEmpty(neutralComment))
                {
                    newTransUnit.SetNoteValue(FromResx, neutralComment);
                }

                if (!string.IsNullOrEmpty(specificCommentText))
                {
                    newTransUnit.SetNoteValue(FromResxSpecific, specificCommentText);
                }

                var nextElement = bodyElement
                    .Descendants(TransUnitElement)
                    .FirstOrDefault(element => StringComparer.Ordinal.Compare(tableEntry.Key, element.GetId()) < 0);

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
    }
}
