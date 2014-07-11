namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Attribute to mark one property as dependent on another property. 
    /// If you call <see cref="ObservableObject.OnPropertyChanged(string)"/> for one property, the property change event will also be raised for all dependent properties. 
    /// </summary>
    /// <example>
    /// <code>
    /// class X : ObservableObject
    /// {
    ///     string Value { get { ... } }
    /// 
    ///     [PropertyDependency("Value")]
    ///     int ValueLength { get { ... } }
    /// 
    ///     void ChageSomething()
    ///     {
    ///         OnPropertyChanged("Value"); 
    ///     }
    /// }
    /// </code>
    /// Calling 'OnPropertyChanged("Value")' will raise the PropertyChanged event for the "Value" property as well as for the dependent "ValueLength" property.
    /// </example>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyDependencyAttribute : Attribute
    {
        private readonly IEnumerable<string> _propertyNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDependencyAttribute"/> class.
        /// </summary>
        /// <param name="propertyNames">The property names of the properties that this property depends on.</param>
        public PropertyDependencyAttribute([Localizable(false)] params string[] propertyNames)
        {
            Contract.Requires(propertyNames != null);
            Contract.Ensures(PropertyNames == propertyNames);

            _propertyNames = propertyNames;
        }

        /// <summary>
        /// Gets the names of the properties that the attributed property depends on.
        /// </summary>
        public IEnumerable<string> PropertyNames
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
                return _propertyNames;
            }
        }

        /// <summary>
        /// Creates the dependency mapping from the attributes of the specified properties.
        /// </summary>
        /// <param name="properties">The properties of the type.</param>
        /// <returns>A dictionary that maps the property names to all direct and indirect dependent property names.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid dependency definitions, i.e. dependency to non-existing property.</exception>
        internal static Dictionary<string, IEnumerable<string>> CreateDependencyMapping(IEnumerable<PropertyInfo> properties)
        {
            Contract.Requires(properties != null);
            Contract.Ensures(Contract.Result<Dictionary<string, IEnumerable<string>>>() != null);

            var dependencyDefinitions = properties.Select(prop => new { prop.Name, DependsUpon = prop.GetCustomAttributes<PropertyDependencyAttribute>(true).SelectMany(attr => attr.PropertyNames).ToArray() }).ToArray();
            var dependencySources = dependencyDefinitions.SelectMany(dependency => dependency.DependsUpon).Distinct().ToArray();

            var invalidDependencyDefinitions = dependencySources.Where(propertyName => !dependencyDefinitions.Select(d => d.Name).Contains(propertyName)).ToArray();
            if (invalidDependencyDefinitions.Any())
                throw new InvalidOperationException(@"Invalid dependency definitions: " + string.Join(", ", invalidDependencyDefinitions));

            var directDependencies = dependencySources.ToDictionary(source => source, source => dependencyDefinitions.Where(dependency => dependency.DependsUpon.Contains(source)).Select(dependency => dependency.Name).ToArray());

            return directDependencies.Keys.ToDictionary(item => item, item => GetAllDependencies(item, directDependencies));
        }

        private static IEnumerable<string> GetAllDependencies(string item, IDictionary<string, string[]> directDependencies)
        {
            Contract.Requires(item != null);
            Contract.Requires(directDependencies != null);

            var allDependenciesAndSelf = new List<string> { item };

            for (var i = 0; i < allDependenciesAndSelf.Count; i++)
            {
                string[] indirectDependencies;

                if (!directDependencies.TryGetValue(allDependenciesAndSelf[i], out indirectDependencies))
                {
                    continue;
                }

                Contract.Assume(indirectDependencies != null);
                allDependenciesAndSelf.AddRange(indirectDependencies.Where(indirectDependency => !allDependenciesAndSelf.Contains(indirectDependency)));
            }

            return allDependenciesAndSelf.Skip(1).ToArray();
        }

        /// <summary>
        /// Gets a list of invalid dependency definitions in the entry types assembly and all referenced assemblies.
        /// </summary>
        /// <param name="entryType">Type of the entry.</param>
        /// <returns>A list of strings, each describing an invalid dependency definition. If no invalid definitions exist, the list is empty.</returns>
        /// <remarks>This method is mainly for writing unit test to detect invalid dependencies during compile time.</remarks>
        public static IEnumerable<string> GetInvalidDependencies(Type entryType)
        {
            Contract.Requires(entryType != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            return from type in entryType.GetCustomAssemblies().SelectMany(a => a.GetTypes())
                   let allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   from property in allProperties
                   let attribute = property.GetCustomAttributes<PropertyDependencyAttribute>(false).FirstOrDefault()
                   where attribute != null
                   let firstInvalidDependency = attribute.PropertyNames.FirstOrDefault(referencedProperty => !allProperties.Any(p => p.Name.Equals(referencedProperty, StringComparison.Ordinal)))
                   where firstInvalidDependency != null
                   select type.FullName + "." + property.Name + " has invalid dependency: " + firstInvalidDependency;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_propertyNames != null);
        }
    }
}
