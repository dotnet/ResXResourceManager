namespace ResXManager.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;

    [DataContract]
    public abstract class ItemTrackingCollectionHost<T> : INotifyChanged
        where T : class, INotifyPropertyChanged
    {
        private ObservableCollection<T> _items;

        private ObservablePropertyChangeTracker<T>? _changeTracker;

        // ReSharper disable once NotNullMemberIsNotInitialized
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        protected ItemTrackingCollectionHost()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            Items = new ObservableCollection<T>();
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by serializer")]
        [DataMember(Name = "Items")]
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

        public event EventHandler? Changed;
    }
}