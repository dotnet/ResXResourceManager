namespace tomenglertde.ResXManager.Model
{
    using System;

    using JetBrains.Annotations;

    public class DelegateIndexer<TKey, TValue>
    {
        [NotNull]
        private readonly Func<TKey, TValue> _getter;

        [NotNull]
        private readonly Action<TKey, TValue> _setter;

        public DelegateIndexer([NotNull] Func<TKey, TValue> getter, [NotNull] Action<TKey, TValue> setter)
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
