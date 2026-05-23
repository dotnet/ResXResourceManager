namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;

public static class Snapshot
{
    public static string CreateSnapshot(this ICollection<ResourceEntity> resourceEntities)
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

    public static void LoadSnapshot(this ICollection<ResourceEntity> resourceEntities, string? snapshot)
    {
        if (snapshot.IsNullOrEmpty())
        {
            UnloadSnapshot(resourceEntities);
        }
        else
        {
            var entitySnapshots = JsonConvert.DeserializeObject<ICollection<EntitySnapshot>>(snapshot) ?? Array.Empty<EntitySnapshot>();
            resourceEntities.Load(entitySnapshots);
        }
    }

    private static void UnloadSnapshot(IEnumerable<ResourceEntity> resourceEntities)
    {
        resourceEntities.SelectMany(entitiy => entitiy.Entries)
            .ForEach(entry => entry.Snapshot = null);
    }

    private static void Load(this IEnumerable<ResourceEntity> resourceEntities, IEnumerable<EntitySnapshot> entitySnapshots)
    {
        var entitySnapshotMap = entitySnapshots
            .GroupBy(snapshot => (snapshot.ProjectName?.ToUpperInvariant(), snapshot.UniqueName?.ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.First());

        resourceEntities.ForEach(entity =>
        {
            var entityKey = (entity.ProjectName?.ToUpperInvariant(), entity.UniqueName?.ToUpperInvariant());
            var entrySnapshots = entitySnapshotMap.TryGetValue(entityKey, out var entitySnapshot)
                ? entitySnapshot.Entries ?? Array.Empty<EntrySnapshot>()
                : Array.Empty<EntrySnapshot>();

            var entrySnapshotMap = entrySnapshots
                .Where(s => s.Key != null)
                .GroupBy(s => s.Key ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Data ?? Array.Empty<DataSnapshot>(), StringComparer.Ordinal);

            entity.Entries.ForEach(entry =>
            {
                var data = entrySnapshotMap.TryGetValue(entry.Key, out var snapData)
                    ? snapData
                    : Array.Empty<DataSnapshot>();

                entry.Snapshot = data.ToDictionary(item => new CultureKey(item.Language), item => new ResourceData { Text = item.Text, Comment = item.Comment });
            });
        });
    }

    private static bool Equals(ResourceEntity entity, EntitySnapshot snapshot)
    {
        return string.Equals(entity.ProjectName, snapshot.ProjectName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(entity.UniqueName, snapshot.UniqueName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? value)
    {
        return value.IsNullOrEmpty() ? null : value;
    }

    [DataContract]
    private sealed class EntitySnapshot
    {
        [DataMember]
        public string? ProjectName
        {
            get;
            set;
        }

        [DataMember]
        public string? UniqueName
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<EntrySnapshot>? Entries
        {
            get;
            set;
        }
    }

    [DataContract]
    private sealed class EntrySnapshot
    {
        [DataMember]
        public string? Key
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<DataSnapshot>? Data
        {
            get;
            set;
        }
    }

    [DataContract]
    private sealed class DataSnapshot
    {
        [DataMember(Name = "L", EmitDefaultValue = false)]
        public string? Language
        {
            get;
            set;
        }

        [DataMember(Name = "C", EmitDefaultValue = false)]
        public string? Comment
        {
            get;
            set;
        }

        [DataMember(Name = "T", EmitDefaultValue = false)]
        public string? Text
        {
            get;
            set;
        }
    }
}