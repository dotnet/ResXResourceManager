namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;

    public static class Snapshot
    {
        public static string CreateSnapshot(this ICollection<ResourceEntity> resourceEntities)
        {
            Contract.Requires(resourceEntities != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var entitySnapshots = resourceEntities.Select(
                entity => new EntitySnapshot
                {
                    ProjectName = entity.ProjectName,
                    UniqueName = entity.UniqueName,
                    Entries = entity.Entries.Select(entry => new EntrySnapshot
                    {
                        Key = entry.Key,
                        Data = entry.Languages.Select(lang => new DataSnapshot
                        {
                            Language = NullIfEmpty(lang.ToString()),
                            Text = NullIfEmpty(entry.Values.GetValue(lang)),
                            Comment = NullIfEmpty(entry.Comments.GetValue(lang)),
                        }).Where(d => d.Text != null || d.Comment != null).ToArray()
                    }).ToArray()
                }).ToArray();

            entitySnapshots.ForEach(entity => Load(resourceEntities, entity));

            return JsonConvert.SerializeObject(entitySnapshots) ?? string.Empty; 
        }

        public static void LoadSnapshot(this ICollection<ResourceEntity> resourceEntities, string snapshot)
        {
            Contract.Requires(resourceEntities != null);

            resourceEntities.SelectMany(entitiy => entitiy.Entries).ForEach(entry => entry.Snapshot = null);

            if (string.IsNullOrEmpty(snapshot))
                return;

            var entitySnapshots = JsonConvert.DeserializeObject<Collection<EntitySnapshot>>(snapshot);
            if (entitySnapshots == null)
                return;

            entitySnapshots.ForEach(entity => Load(resourceEntities, entity));
        }

        private static void Load(IEnumerable<ResourceEntity> resourceEntities, EntitySnapshot entitySnapshot)
        {
            Contract.Requires(resourceEntities != null);
            Contract.Requires(entitySnapshot != null);

            var entrySnapshots = entitySnapshot.Entries;
            if (entrySnapshots == null)
                return;

            var entity = resourceEntities.FirstOrDefault(e => string.Equals(e.ProjectName, entitySnapshot.ProjectName, StringComparison.OrdinalIgnoreCase) && string.Equals(e.UniqueName, entitySnapshot.UniqueName, StringComparison.OrdinalIgnoreCase));
            if (entity == null)
                return;

            entrySnapshots.ForEach(entry => Load(entity.Entries, entry));
        }

        private static void Load(IEnumerable<ResourceTableEntry> entries, EntrySnapshot entrySnapshot)
        {
            Contract.Requires(entries != null);
            Contract.Requires(entrySnapshot != null);

            var data = entrySnapshot.Data;
            if (data == null)
                return;

            var entry = entries.FirstOrDefault(e => string.Equals(e.Key, entrySnapshot.Key));
            if (entry == null)
                return;

            entry.Snapshot = data.ToDictionary(item => new CultureKey(item.Language), item => new ResourceData {Text = item.Text, Comment = item.Comment});
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    [DataContract]
    public class EntitySnapshot
    {
        [DataMember]
        public string ProjectName
        {
            get;
            set;
        }
        [DataMember]
        public string UniqueName
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<EntrySnapshot> Entries
        {
            get;
            set;
        }
    }

    [DataContract]
    public class EntrySnapshot
    {
        [DataMember]
        public string Key
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<DataSnapshot> Data
        {
            get;
            set;
        }
    }

    [DataContract]
    public class DataSnapshot
    {
        [DataMember(Name = "L", EmitDefaultValue = false)]
        public string Language
        {
            get;
            set;
        }

        [DataMember(Name = "C", EmitDefaultValue = false)]
        public string Comment
        {
            get;
            set;
        }

        [DataMember(Name = "T", EmitDefaultValue = false)]
        public string Text
        {
            get;
            set;
        }
    }
}
