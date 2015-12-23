namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
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

            resourceEntities.Load(entitySnapshots);

            return JsonConvert.SerializeObject(entitySnapshots) ?? string.Empty;
        }

        public static void LoadSnapshot(this ICollection<ResourceEntity> resourceEntities, string snapshot)
        {
            Contract.Requires(resourceEntities != null);

            if (string.IsNullOrEmpty(snapshot))
            {
                UnloadSnapshot(resourceEntities);
            }
            else
            {
                var entitySnapshots = JsonConvert.DeserializeObject<ICollection<EntitySnapshot>>(snapshot) ?? new EntitySnapshot[0];
                resourceEntities.Load(entitySnapshots);
            }
        }

        private static void UnloadSnapshot(IEnumerable<ResourceEntity> resourceEntities)
        {
            Contract.Requires(resourceEntities != null);

            resourceEntities.SelectMany(entitiy => entitiy.Entries)
                .ForEach(entry => entry.Snapshot = null);
        }

        private static void Load(this IEnumerable<ResourceEntity> resourceEntities, IEnumerable<EntitySnapshot> entitySnapshots)
        {
            Contract.Requires(resourceEntities != null);
            Contract.Requires(entitySnapshots != null);

            resourceEntities.ForEach(entity =>
            {
                var entrySnapshots = entitySnapshots.Where(snapshot => Equals(entity, snapshot)).Select(s => s.Entries).FirstOrDefault() ?? new EntrySnapshot[0];

                entity.Entries.ForEach(entry =>
                {
                    var data = entrySnapshots.Where(s => string.Equals(entry.Key, s.Key)).Select(s => s.Data).FirstOrDefault() ?? new DataSnapshot[0];

                    entry.Snapshot = data.ToDictionary(item => new CultureKey(item.Language), item => new ResourceData { Text = item.Text, Comment = item.Comment }); ;
                });
            });
        }

        private static bool Equals(ResourceEntity entity, EntitySnapshot snapshot)
        {
            Contract.Requires(entity != null);
            Contract.Requires(snapshot != null);

            return string.Equals(entity.ProjectName, snapshot.ProjectName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(entity.UniqueName, snapshot.UniqueName, StringComparison.OrdinalIgnoreCase);
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [DataContract]
        class EntitySnapshot
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
        class EntrySnapshot
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
        class DataSnapshot
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
}
