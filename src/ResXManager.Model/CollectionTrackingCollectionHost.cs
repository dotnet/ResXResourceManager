namespace ResXManager.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Tracks added/removed items but not their properties
    /// </summary>
    [DataContract]
    public abstract class CollectionTrackingCollectionHost<T> : INotifyChanged
    {
        // ! must be initialized via property setter on Items in constructor.
        private ObservableCollection<T> _items = null!;

        protected CollectionTrackingCollectionHost()
        {
            Items = new ObservableCollection<T>();
        }

        [DataMember(Name = "Items")]
        public ObservableCollection<T> Items
        {
            get => _items;
            set
            {
                if (_items != null)
                    throw new InvalidOperationException("Items must only be set once, either by the constructor or by the serializer!");

                _items = value;
                _items.CollectionChanged += (sender, e) => Changed?.Invoke(this, e);
            }
        }

        public event EventHandler? Changed;
    }
}
