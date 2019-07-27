namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;

    [DataContract]
    public abstract class ItemTrackingCollectionHost<T> : INotifyChanged
        where T : class, INotifyPropertyChanged
    {
        [NotNull, ItemNotNull]
        private ObservableCollection<T> _items;
        [CanBeNull]
        private ObservablePropertyChangeTracker<T> _changeTracker;

        // ReSharper disable once NotNullMemberIsNotInitialized
        protected ItemTrackingCollectionHost()
        {
            Items = new ObservableCollection<T>();
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by serializer")]
        [DataMember(Name = "Items")]
        [NotNull, ItemNotNull]
        public ObservableCollection<T> Items
        {
            get => _items;
            set
            {
                if (_changeTracker != null)
                    throw new InvalidOperationException("Items must only be set once, either by the constructor or by the serializer!");

                _items = value;
                _changeTracker = new ObservablePropertyChangeTracker<T>(_items);
                _changeTracker.ItemPropertyChanged += (sender, e) => Changed?.Invoke(this, e);
            }
        }

        public event EventHandler Changed;
    }
}