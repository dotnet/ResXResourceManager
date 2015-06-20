namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Helper to control the lifetime of com objects via the using pattern.
    /// </summary>
    public sealed class ComObject : IDisposable
    {
        private readonly object _item;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComObject"/> class.
        /// </summary>
        /// <param name="item">The com object to control.</param>
        /// <exception cref="InvalidComObjectException">The item is not a valid com object.</exception>
        private ComObject(object item)
        {
            Contract.Requires(item != null);

            if (!Marshal.IsComObject(item))
                throw new InvalidComObjectException();

            this._item = item;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ComObject"/> class.
        /// </summary>
        /// <param name="item">The com object to control.</param>
        /// <returns>An IDisposable object to release the com object.</returns>
        /// <exception cref="InvalidComObjectException">The item is not a valid com object.</exception>
        /// <example><code>
        /// var item = new AnyComObject();
        /// using (GetLifetimeService(item))
        /// {
        ///     item.DoSomething();
        ///     /* The item will be automatically released when leaving this scope. */
        /// }
        /// </code></example>
        public static IDisposable GetLifetimeService(object item)
        {
            return (item != null) ? new ComObject(item) : null;
        }

        /// <summary>
        /// Releases the com object.
        /// </summary>
        public void Dispose()
        {
            Marshal.ReleaseComObject(_item);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_item != null);
        }
    }
}