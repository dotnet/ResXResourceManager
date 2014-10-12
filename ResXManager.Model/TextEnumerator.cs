namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Text;

    /// <summary>
    /// Enumerator for strings, providing access to current and next char, and some usefull iterator extensions.
    /// </summary>
    public sealed class TextEnumerator : IDisposable
    {
        private const char EOS = '\0';
        private readonly IEnumerator<char> _inner;
        private char _current;
        private char _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextEnumerator"/> class.
        /// </summary>
        /// <param name="inner">The base enumerator.</param>
        public TextEnumerator(IEnumerator<char> inner)
        {
            Contract.Requires(inner != null);

            _inner = inner;

            MoveNext();
            MoveNext();
        }

        /// <summary>
        /// Gets the current char.
        /// </summary>
        public char Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Gets the next char.
        /// </summary>
        public char Next
        {
            get
            {
                return _next;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is more data to read, i.e. current is not beyond EOS.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has data; otherwise, <c>false</c>.
        /// </value>
        public bool HasData
        {
            get
            {
                return Current != EOS;
            }
        }

        /// <summary>
        /// Skips the specified number of characters.
        /// </summary>
        /// <param name="count">The count.</param>
        public void Skip(int count)
        {
            while (HasData && (count > 0))
            {
                MoveNext();
                count -= 1;
            }
        }

        /// <summary>
        /// Skips the while the predicate returns true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public void SkipWhile(Func<char, bool> predicate)
        {
            Contract.Requires(predicate != null);

            while (HasData && predicate(Current))
            {
                MoveNext();
            }
        }

        /// <summary>
        /// Takes characters from the stream until the predicate returns true.
        /// </summary>
        /// <param name="predicate">The predicate testing the current char.</param>
        /// <returns>The characters taken from the stream.</returns>
        public string TakeUntil(Func<char, bool> predicate)
        {
            Contract.Requires(predicate != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var text = new StringBuilder();

            while (HasData && !predicate(Current))
            {
                text.Append(Current);
                MoveNext();
            }

            return text.ToString();
        }

        /// <summary>
        /// Takes characters from the stream until the counter returns 0.
        /// </summary>
        /// <param name="chunkCounter">A function returning the number of characters to take for every chunk.</param>
        /// <returns>The characters taken from the stream.</returns>
        public string Take(Func<char, char, int> chunkCounter)
        {
            Contract.Requires(chunkCounter != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var text = new StringBuilder();

            while (HasData)
            {
                var n = chunkCounter(Current, Next);

                if (n == 0)
                    break;

                while (HasData && (n > 0))
                {
                    text.Append(Current);
                    MoveNext();
                    n -= 1;
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// Moves the enumerator to the next character.
        /// </summary>
        public void MoveNext()
        {
            _current = _next;
            _next = _inner.MoveNext() ? _inner.Current : '\0';
        }

        #region IDisposable Members

        public void Dispose()
        {
            _inner.Dispose();
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_inner != null);
        }

    }
}
