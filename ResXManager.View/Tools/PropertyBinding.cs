namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Support binding of elements when the target is not a <see cref="DependencyObject"/>
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    public class PropertyBinding<T>
    {
        private readonly BindingHelper _bindingHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyBinding{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="path">The path.</param>
        public PropertyBinding(object source, string path)
            : this(source, BindingMode.OneWay, path)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyBinding{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyPath">The property path.</param>
        public PropertyBinding(object source, PropertyPath propertyPath)
            : this(source, BindingMode.OneWay, propertyPath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyBinding{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="path">The path.</param>
        public PropertyBinding(object source, BindingMode mode, string path)
            : this(source, mode, new PropertyPath(path))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyBinding{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="propertyPath">The property path.</param>
        public PropertyBinding(object source, BindingMode mode, PropertyPath propertyPath)
        {
            _bindingHelper = new BindingHelper(this);
            BindingOperations.SetBinding(_bindingHelper, BindingHelper.ValueProperty, new Binding { Path = propertyPath, Source = source, Mode = mode });
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T Value
        {
            get { return (T)_bindingHelper.GetValue(BindingHelper.ValueProperty); }
            set { _bindingHelper.SetValue(BindingHelper.ValueProperty, value); }
        }

        /// <summary>
        /// Occurs when the value has changed.
        /// </summary>
        public event EventHandler<PropertyBindingValueChangedEventArgs<T>> ValueChanged;

        private void Value_Changed(T oldValue, T newValue)
        {
            var eventHandler = ValueChanged;

            if (eventHandler != null)
            {
                eventHandler(this, new PropertyBindingValueChangedEventArgs<T>(oldValue, newValue));
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Detach()
        {
            BindingOperations.ClearBinding(_bindingHelper, BindingHelper.ValueProperty);
        }

        private class BindingHelper : DependencyObject
        {
            private readonly PropertyBinding<T> _owner;

            public BindingHelper(PropertyBinding<T> owner)
            {
                Contract.Requires(owner != null);
                _owner = owner;
            }

            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(T), typeof(BindingHelper), new FrameworkPropertyMetadata((sender, e) => ((BindingHelper)sender)._owner.Value_Changed((T)e.OldValue, (T)e.NewValue)));

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_owner != null);
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_bindingHelper != null);
        }
    }
}
