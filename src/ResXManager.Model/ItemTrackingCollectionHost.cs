namespace ResXManager.Model;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

using TomsToolbox.Essentials;
using TomsToolbox.ObservableCollections;

/// <summary>
/// Tracks properties of items
/// </summary>
[DataContract]
public abstract class ItemTrackingCollectionHost<T> : INotifyChanged
    where T : class, INotifyPropertyChanged
{
    // ! must be initialized via property setter on Items in constructor.
    private ObservableCollection<T> _items = null!;

    private ObservablePropertyChangeTracker<T>? _changeTracker;

    protected ItemTrackingCollectionHost()
    {
        Items = new ObservableCollection<T>();
    }

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