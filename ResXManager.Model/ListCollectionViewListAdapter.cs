namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows.Data;

    /// <summary>
    /// Adapter for a <see cref="ListCollectionView"/> that exposes the content as a readonly collection with an IList interface.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1035:ICollectionImplementationsHaveStronglyTypedMembers", Justification = "ListCollectionView is not strongly typed."), SuppressMessage("Microsoft.Design", "CA1039:ListsAreStronglyTyped", Justification = "ListCollectionView is not strongly typed.")]
    [ContractVerification(false)] // Simply forwarding inner list.
    public sealed class ListCollectionViewListAdapter : IList, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly ListCollectionView _collectionView;

        public ListCollectionViewListAdapter(ListCollectionView collectionView)
        {
            Contract.Requires(collectionView != null);
            _collectionView = collectionView;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_collectionView).GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (index + array.Length < Count)
                throw new ArgumentException("array is too small");
            if (array.Rank != 1)
                throw new ArgumentException("array is not one-dimensional");

            foreach (var item in _collectionView)
            {
                Contract.Assume(index < array.Length);
                array.SetValue(item, index++);
            }
        }

        public Predicate<object> Filter
        {
            get
            {
                return _collectionView.Filter;
            }
            set
            {
                _collectionView.Filter = value;
            }
        }

        public ListCollectionView CollectionView
        {
            get
            {
                Contract.Ensures(Contract.Result<ListCollectionView>() != null);

                return _collectionView;
            }
        }

        public int Count
        {
            get
            {
                return _collectionView.Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return _collectionView;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        int IList.Add(object value)
        {
            ReadOnlyNotSupported();
            return 0;
        }

        public bool Contains(object value)
        {
            return _collectionView.Contains(value);
        }

        void IList.Clear()
        {
            ReadOnlyNotSupported();
        }

        public int IndexOf(object value)
        {
            return _collectionView.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ReadOnlyNotSupported();
        }

        void IList.Remove(object value)
        {
            ReadOnlyNotSupported();
        }

        void IList.RemoveAt(int index)
        {
            ReadOnlyNotSupported();
        }

        public object this[int index]
        {
            get
            {
                return _collectionView.GetItemAt(index);
            }
            set
            {
                if (value == null) 
                    throw new ArgumentNullException("value");

                ReadOnlyNotSupported();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        private static void ReadOnlyNotSupported()
        {
            throw new NotSupportedException(@"Collection is read-only.");
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                ((INotifyPropertyChanged)_collectionView).PropertyChanged += value;
            }
            remove
            {
                ((INotifyPropertyChanged)_collectionView).PropertyChanged -= value;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)_collectionView).CollectionChanged += value;
            }
            remove
            {
                ((INotifyCollectionChanged)_collectionView).CollectionChanged -= value;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_collectionView != null);
        }
    }
}
