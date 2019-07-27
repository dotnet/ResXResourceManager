namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    public static class Snapshot
    {
        [NotNull]
        public static string CreateSnapshot([NotNull][ItemNotNull] this ICollection<ResourceEntity> resourceEntities)
        {
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

            resourceEntities.Load(entitySnapshots);

            return JsonConvert.SerializeObject(entitySnapshots) ?? string.Empty;
        }

        public static void LoadSnapshot([NotNull][ItemNotNull] this ICollection<ResourceEntity> resourceEntities, [CanBeNull] string snapshot)
        {
            if (string.IsNullOrEmpty(snapshot))
            {
                UnloadSnapshot(resourceEntities);
            }
            else
            {
                var entitySnapshots = JsonConvert.DeserializeObject<ICollection<EntitySnapshot>>(snapshot) ?? Array.Empty<EntitySnapshot>();
                resourceEntities.Load(entitySnapshots);
            }
        }

        private static void UnloadSnapshot([NotNull][ItemNotNull] IEnumerable<ResourceEntity> resourceEntities)
        {
            resourceEntities.SelectMany(entitiy => entitiy.Entries)
                .ForEach(entry => entry.Snapshot = null);
        }

        private static void Load([NotNull][ItemNotNull] this IEnumerable<ResourceEntity> resourceEntities, [NotNull][ItemNotNull] IEnumerable<EntitySnapshot> entitySnapshots)
        {
            resourceEntities.ForEach(entity =>
            {
                var entrySnapshots = entitySnapshots.Where(snapshot => Equals(entity, snapshot)).Select(s => s.Entries).FirstOrDefault() ?? Array.Empty<EntrySnapshot>();

                entity.Entries.ForEach(entry =>
                {
                    var data = entrySnapshots.Where(s => string.Equals(entry.Key, s.Key, StringComparison.Ordinal)).Select(s => s.Data).FirstOrDefault() ?? Array.Empty<DataSnapshot>();

                    entry.Snapshot = data.ToDictionary(item => new CultureKey(item.Language), item => new ResourceData { Text = item.Text, Comment = item.Comment });
                });
            });
        }

        private static bool Equals([NotNull] ResourceEntity entity, [NotNull] EntitySnapshot snapshot)
        {
            return string.Equals(entity.ProjectName, snapshot.ProjectName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(entity.UniqueName, snapshot.UniqueName, StringComparison.OrdinalIgnoreCase);
        }

        [CanBeNull]
        private static string NullIfEmpty([CanBeNull] string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [DataContract]
        private class EntitySnapshot
        {
            [CanBeNull]
            [DataMember]
            public string ProjectName
            {
                get;
                set;
            }

            [CanBeNull]
            [DataMember]
            public string UniqueName
            {
                get;
                set;
            }

            [CanBeNull]
            [ItemNotNull]
            [DataMember]
            public ICollection<EntrySnapshot> Entries
            {
                get;
                set;
            }
        }

        [DataContract]
        private class EntrySnapshot
        {
            [CanBeNull]
            [DataMember]
            public string Key
            {
                get;
                set;
            }

            [CanBeNull]
            [ItemNotNull]
            [DataMember]
            public ICollection<DataSnapshot> Data
            {
                get;
                set;
            }
        }

        [DataContract]
        private class DataSnapshot
        {
            [CanBeNull]
            [DataMember(Name = "L", EmitDefaultValue = false)]
            public string Language
            {
                get;
                set;
            }

            [CanBeNull]
            [DataMember(Name = "C", EmitDefaultValue = false)]
            public string Comment
            {
                get;
                set;
            }

            [CanBeNull]
            [DataMember(Name = "T", EmitDefaultValue = false)]
            public string Text
            {
                get;
                set;
            }
        }
    }
}
