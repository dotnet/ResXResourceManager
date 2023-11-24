namespace ResXManager.Model
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    public sealed class RecentFolderConfigurationItem : INotifyPropertyChanged
    {
        [DataMember]
        public string Folder { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public RecentFolderConfigurationItem(string folder)
        {
            Folder = folder;
        }
    }

    [KnownType(typeof(RecentFolderConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<RecentFolderConfiguration>))]
    public sealed class RecentFolderConfiguration : CollectionTrackingCollectionHost<RecentFolderConfigurationItem>
    {
        private const int MaxRecentFolders = 10;
        public const string Default = @"{""Items"":[]}";

        public void Add(string folder)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Folder == folder)
                    Items.RemoveAt(i);
            }

            Items.Insert(0, new RecentFolderConfigurationItem(folder));

            while (Items.Count > MaxRecentFolders)
            {
                Items.RemoveAt(Items.Count - 1);
            }
        }
    }
}
