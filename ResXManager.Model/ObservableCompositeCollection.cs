namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    /// View a set of collections as one continuous list.
    /// <para/>
    /// Similar to the <see cref="System.Windows.Data.CompositeCollection"/>, plus:
    /// <list type="bullet">
    /// <item>generic type</item>
    /// <item>transparent separation of the real content and the resulting list</item>
    /// <item>nestable, i.e. composite collections of composite collections</item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">Type of the items in the collection</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ObservableCompositeCollection<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
        where T : class
    {
        [ContractPublicPropertyName(@"Content")]
        private readonly ContentManager _content;

        /// <summary>
        /// Taking care of the physical content
        /// </summary>
        private class ContentManager : IList<IList>, INotifyCollectionChanged
        {
            // The parts that make up the composite collection
            private readonly List<IList> _parts = new List<IList>();
            // The composite collection that we manage
            private readonly ObservableCompositeCollection<T> _owner;

            public ContentManager(ObservableCompositeCollection<T> owner)
            {
                Contract.Requires(owner != null);

                _owner = owner;
            }

            private void parts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                _owner.ContentChanged();

                // Monitor changes of parts and forward events properly
                if (CollectionChanged == null)
                    return;

                // Offset to apply is the sum of all counts of all parts preceding this part
                var offset = (_parts.TakeWhile(part => !part.Equals(sender)).Select(part => part.Count)).Sum();
                CollectionChanged(_owner, TranslateEventArgs(e, offset));
            }

            private static NotifyCollectionChangedEventArgs TranslateEventArgs(NotifyCollectionChangedEventArgs e, int offset)
            {
                // Translate given event args by adding the given offset to the starting index
                Contract.Requires(e != null);

                if (offset == 0)
                    return e; // no offset - nothing to translate

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        return new NotifyCollectionChangedEventArgs(e.Action, e.NewItems, e.NewStartingIndex + offset);

                    case NotifyCollectionChangedAction.Reset:
                        return e;

                    case NotifyCollectionChangedAction.Remove:
                        return new NotifyCollectionChangedEventArgs(e.Action, e.OldItems, e.OldStartingIndex + offset);

                    default:
                        throw new NotImplementedException();
                }
            }

            #region IList<IList> Members

            [ContractVerification(false)] // Just forwarding...
            public int IndexOf(IList item)
            {
                return _parts.IndexOf(item);
            }

            public void Insert(int index, IList item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");
                if (index > _parts.Count)
                    throw new ArgumentOutOfRangeException("index");

                // now add this list to the list of all
                _parts.Insert(index, item);
                _owner.ContentChanged();

                // If this list implements INotifyCollectionChanged monitor changes so they can be forwarded properly.
                // If it does not implement INotifyCollectionChanged, no notification should be necessary.
                var dynamicPart = item as INotifyCollectionChanged;
                if (dynamicPart != null)
                    dynamicPart.CollectionChanged += parts_CollectionChanged;

                if (item == null)
                    throw new ArgumentNullException("item");

                // If this new part contains items and someone registered for changes, raise change events
                if ((item.Count > 0) && (CollectionChanged != null))
                {
                    // calculate the absolute offset of the first item (= sum of all preceding items in all lists before the new item)
                    var offset = (_parts.GetRange(0, index).Select(p => p.Count)).Sum();

                    // Send single notifications, ListCollectionView does not support multi-item changes!
                    foreach (T element in item)
                    {
                        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, element, offset);
                        CollectionChanged(_owner, args);
                        offset += 1;
                    }
                }
            }

            [ContractVerification(false)] // Just forwarding...
            public void RemoveAt(int index)
            {
                var part = _parts[index];
                Contract.Assume(part != null);
                _parts.RemoveAt(index);
                _owner.ContentChanged();

                // If this list implements INotifyCollectionChanged events must be unregistered!
                var dynamicPart = part as INotifyCollectionChanged;
                if (dynamicPart != null)
                    dynamicPart.CollectionChanged -= parts_CollectionChanged;

                if ((part.Count <= 0) || (CollectionChanged == null))
                    return;

                // calculate the absolute offset of the first item (sum of all items in all lists before the removed item)
                var offset = (_parts.GetRange(0, index).Select(p => p.Count)).Sum();

                foreach (T item in part)
                {
                    // offset stays, items are bubbling up!
                    CollectionChanged(_owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, offset));
                }
            }

            [ContractVerification(false)] // Just forwarding...
            public IList this[int index]
            {
                get
                {
                    return _parts[index];
                }
                set
                {
                    RemoveAt(index);
                    Insert(index, value);
                }
            }

            #endregion

            #region ICollection<IList> Members

            [ContractVerification(false)] // just forwarding
            public void Add(IList item)
            {
                Insert(_parts.Count, item);
            }

            public void Clear()
            {
                while (Count > 0)
                {
                    RemoveAt(0);
                }
            }

            public bool Contains(IList item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                return IndexOf(item) != -1;
            }

            public void CopyTo(IList[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get
                {
                    return _parts.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool Remove(IList item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                var index = IndexOf(item);
                if (index == -1)
                    return false;
                RemoveAt(index);
                return true;
            }

            #endregion

            #region IEnumerable<IList> Members

            public IEnumerator<IList> GetEnumerator()
            {
                return _parts.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _parts.GetEnumerator();
            }

            #endregion

            #region INotifyCollectionChanged Members

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            #endregion

            #region Contracts Invariant

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_owner != null);
                Contract.Invariant(_parts != null);
            }

            #endregion
        }

        /// <summary>
        /// Create an empty collection
        /// </summary>
        public ObservableCompositeCollection()
        {
            _content = new ContentManager(this);
        }

        /// <summary>
        /// Create a collection initially containing one single item
        /// </summary>
        /// <param name="singleItem">The initial item in the collection</param>
        public ObservableCompositeCollection(T singleItem)
            : this()
        {
            Contract.Requires(singleItem != null);

            var list = new T[] { singleItem };
            _content.Add(list);
        }

        /// <summary>
        /// Create a collection wrapping a list of lists.
        /// </summary>
        /// <param name="parts">The lists</param>
        public ObservableCompositeCollection(params IList[] parts)
            : this()
        {
            Contract.Requires(parts != null);

            _content.AddRange(parts.Where(part => part != null));
        }

        /// <summary>
        /// Access to the physical layer of the content
        /// </summary>
        public IList<IList> Content
        {
            get
            {
                return _content;
            }
        }

        private void ContentChanged()
        {
            OnPropertyChanged(@"Count");
        }

        #region Implementation of the interfaces to get a flat read only collection of all parts

        /// <summary>
        /// General handling for all interface functions not supported because we are read only.
        /// </summary>
        private static void ReadOnlyNotSupported()
        {
            throw new NotSupportedException(@"Collection is read-only.");
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _content.SelectMany(list => list.Cast<T>()).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IList<T> Members

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"></see>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"></see>.</param>
        /// <returns>
        /// The index of item if found in the list; otherwise, -1.
        /// </returns>
        [ContractVerification(false)]
        public int IndexOf(T item)
        {
            var offset = 0;
            foreach (var list in _content)
            {
                var index = list.IndexOf(item);
                if (index >= 0)
                    return index + offset;
                offset += list.Count;
            }
            return -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            ReadOnlyNotSupported();
        }

        [ContractVerification(false)]
        void IList<T>.RemoveAt(int index)
        {
            ReadOnlyNotSupported();
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"></see>.</exception>
        [ContractVerification(false)]
        public T this[int index]
        {
            get
            {
                Contract.Requires(index >= 0);

                foreach (var list in _content)
                {
                    Contract.Assume(list != null);

                    if (index < list.Count)
                        return (T)list[index];
                    index -= list.Count;
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("index");
                }

                throw new ArgumentOutOfRangeException("index");
            }
        }

        [ContractVerification(false)]
        T IList<T>.this[int index]
        {
            get
            {
                return this[index];
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
                ReadOnlyNotSupported();
            }
        }

        #endregion

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            ReadOnlyNotSupported();
        }

        [ContractVerification(false)]
        void ICollection<T>.Clear()
        {
            ReadOnlyNotSupported();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
        /// <returns>
        /// true if item is found in the <see cref="T:System.Collections.Generic.ICollection`1"></see>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        [ContractVerification(false)]
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (arrayIndex + array.Length < Count)
                throw new ArgumentException("array is too small");

            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        /// <returns>The number of elements contained in the collection.</returns>
        [ContractVerification(false)] // Validation: Sum of positive numbers is always positive.
        public int Count
        {
            get
            {
                return _content.Select(list => list.Count).Sum();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            ReadOnlyNotSupported();
            return false;
        }

        #endregion

        #region IList Members

        [ContractVerification(false)]
        int IList.Add(object value)
        {
            ReadOnlyNotSupported();
            return 0;
        }

        [ContractVerification(false)]
        void IList.Clear()
        {
            ReadOnlyNotSupported();
        }

        bool IList.Contains(object value)
        {
            return IndexOf(value as T) != -1;
        }

        [ContractVerification(false)]
        int IList.IndexOf(object value)
        {
            return IndexOf(value as T);
        }

        void IList.Insert(int index, object value)
        {
            ReadOnlyNotSupported();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.IList"/> has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        void IList.Remove(object value)
        {
            ReadOnlyNotSupported();
        }

        void IList.RemoveAt(int index)
        {
            ReadOnlyNotSupported();
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
                ReadOnlyNotSupported();
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (index + array.Length < Count)
                throw new ArgumentException("array is too small");
            if (array.Rank != 1)
                throw new ArgumentException("array is not one-dimensional");

            foreach (var item in this)
            {
                Contract.Assume(index < array.Length);
                array.SetValue(item, index++);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.</returns>
        public object SyncRoot
        {
            get
            {
                return this;
            }
        }


        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                _content.CollectionChanged += value;
            }
            remove
            {
                _content.CollectionChanged -= value;
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #endregion  // Interface implementations

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_content != null);
        }
    }
}