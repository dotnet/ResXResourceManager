namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;

    ///<summary>
    /// Provides support for extracting property information based on a property expression.
    ///</summary>
    public static class PropertySupport
    {
        /// <summary>
        /// Extracts the property name from a property expression.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression (e.g. p => p.PropertyName) to extract the property name from.</param>
        /// <returns>The name of the property.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="propertyExpression"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the expression is:<br/>
        ///     Not a <see cref="MemberExpression"/><br/>
        ///     The <see cref="MemberExpression"/> does not represent a property.<br/>
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification="Works only with exactly this kind of expression, so we don't want to allow to pass something else!")]
        public static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);
            Contract.Ensures(!String.IsNullOrEmpty(Contract.Result<string>()));

            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException(@"Expression is not a member access expression", "propertyExpression");
            }

            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new ArgumentException(@"Expression is not a property expression", "propertyExpression");
            }

            var memberName = memberExpression.Member.Name;
            if (string.IsNullOrEmpty(memberName))
            {
                throw new ArgumentException(@"Expression is not a valid property expression", "propertyExpression");
            }

            return memberName;
        }

        /// <summary>
        /// Gets the <see cref="PropertyChangedEventArgs"/> for the specified property.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression (e.g. p => p.PropertyName) to extract the property name from.</param>
        /// <returns>The event args to pass to <see cref="INotifyPropertyChanged.PropertyChanged"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the expression is:<br/>
        ///     Not a <see cref="MemberExpression"/><br/>
        ///     The <see cref="MemberExpression"/> does not represent a property.<br/>
        /// </exception>
        public static PropertyChangedEventArgs GetEventArgs<T>(Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);
            Contract.Ensures(Contract.Result<PropertyChangedEventArgs>() != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<PropertyChangedEventArgs>().PropertyName));

            var args = new PropertyChangedEventArgs(ExtractPropertyName(propertyExpression));

            Contract.Assume(!string.IsNullOrEmpty(args.PropertyName));

            return args;
        }
    }
}
