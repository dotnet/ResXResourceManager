namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Windows.Threading;
    using JetBrains.Annotations;

    /// <summary>
    /// Base class implementing INotifyPropertyChanged. 
    /// Supports declarative dependencies specified by the <see cref="PropertyDependencyAttribute"/>
    /// </summary>
    public abstract class ObservableObject : DispatcherObject, INotifyPropertyChanged
    {
        private Dictionary<string, IEnumerable<string>> _dependencyMapping;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the property identified by the specified property expression.
        /// </summary>
        /// <param name="propertyExpression">The expression identifying the property.</param>
        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            OnPropertyChanged(PropertySupport.ExtractPropertyName(propertyExpression));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        // [CallerMemberName] only available in .Net 4.5! Remove the comments as soon as the project is migrated.
        // ; omit this parameter to use the <see cref="CallerMemberNameAttribute"/></param>
        // [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This pattern is required by the CallerMemberName attribute.")]
        protected void OnPropertyChanged(/*[CallerMemberName]*/ string propertyName /*= null*/)
        {
            InternalOnPropertyChanged(propertyName);

            IEnumerable<string> dependentProperties;

            var dependencyMapping = _dependencyMapping ?? (_dependencyMapping = PropertyDependencyAttribute.CreateDependencyMapping(GetType().GetProperties()));

            if (!dependencyMapping.TryGetValue(propertyName, out dependentProperties))
                return;

            Contract.Assume(dependentProperties != null);

            foreach (var dependentProperty in dependentProperties)
            {
                Contract.Assume(dependentProperty != null);
                InternalOnPropertyChanged(dependentProperty);
            }
        }

        private void InternalOnPropertyChanged(string propertyName)
        {
            var eventHander = this.PropertyChanged;
            if (eventHander != null)
            {
                eventHander(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
