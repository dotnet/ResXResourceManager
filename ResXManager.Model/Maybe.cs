namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;

    public static class MaybeExtensions
    {
        public static Maybe<T> Maybe<T>(this T source)
            where T: class
        {
            Contract.Ensures(Contract.Result<Maybe<T>>() != null);

            return new Maybe<T>(source);
        }
    }

    public class Maybe<T>
        where T : class
    {
        private readonly T _source;

        public Maybe(T source)
        {
            _source = source;
        }

        public Maybe<TTarget> Select<TTarget>(Func<T, TTarget> selector)
            where TTarget : class
        {
            Contract.Requires(selector != null);
            Contract.Ensures(Contract.Result<Maybe<TTarget>>() != null);

            return new Maybe<TTarget>((_source == null) ? null : selector(_source));
        }

        public TTarget Return<TTarget>(Func<T, TTarget> selector)
        {
            Contract.Requires(selector != null);

            return Return(selector, default(TTarget));
        }

        public TTarget Return<TTarget>(Func<T, TTarget> selector, TTarget fallbackValue)
        {
            Contract.Requires(selector != null);

            return (_source == null) ? fallbackValue : selector(_source);
        }

        public Maybe<T> Do(Action<T> action)
        {
            Contract.Requires(action != null);
            Contract.Ensures(Contract.Result<Maybe<T>>() != null);

            if (_source != null)
            {
                action(_source);
            }

            return this;
        }

        public Maybe<T> If(Func<T, bool> condition)
        {
            Contract.Requires(condition != null);
            Contract.Ensures(Contract.Result<Maybe<T>>() != null);

            if ((_source != null) && (condition(_source)))
            {
                return this;
            }

            return new Maybe<T>(null);
        }
    }
}
