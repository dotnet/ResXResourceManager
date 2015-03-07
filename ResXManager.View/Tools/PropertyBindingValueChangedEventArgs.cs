namespace tomenglertde.ResXManager.View.Tools
{
    using System;

    /// <summary>
    /// Event arguments for the <see cref="PropertyBinding{T}.ValueChanged"/> event.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    public class PropertyBindingValueChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyBindingValueChangedEventArgs{T}"/> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public PropertyBindingValueChangedEventArgs(T oldValue, T newValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        /// <summary>
        /// Gets the old value.
        /// </summary>
        public T OldValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public T NewValue
        {
            get;
            private set;
        }
    }
}