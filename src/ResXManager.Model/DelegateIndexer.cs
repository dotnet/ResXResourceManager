namespace ResXManager.Model
{
    using System;

    public class DelegateIndexer<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _getter;

        private readonly Action<TKey, TValue> _setter;

        public DelegateIndexer(Func<TKey, TValue> getter, Action<TKey, TValue> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public TValue this[TKey key]
        {
            get => _getter(key);
            set => _setter(key, value);
        }
    }
}
