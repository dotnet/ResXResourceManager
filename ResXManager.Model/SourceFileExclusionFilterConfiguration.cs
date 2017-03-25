namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using TomsToolbox.ObservableCollections;

    [DataContract]
    public class SourceFileExclusionFilterConfigurationItem : INotifyPropertyChanged
    {
        private string _expression;

        [DataMember]
        public string Expression
        {
            get
            {
                return _expression;
            }
            set
            {
                SetProperty(ref _expression, value, nameof(Expression));
            }
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void SetProperty<T>(ref T backingField, T value, [NotNull] string propertyName)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));

            if (Equals(backingField, value))
                return;

            backingField = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    [KnownType(typeof(SourceFileExclusionFilterConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<SourceFileExclusionFilterConfiguration>))]
    public class SourceFileExclusionFilterConfiguration
    {
        private ObservableCollection<SourceFileExclusionFilterConfigurationItem> _items;
        private ObservablePropertyChangeTracker<SourceFileExclusionFilterConfigurationItem> _changeTracker;

        [DataMember(Name = "Items")]
        [NotNull]
        public ObservableCollection<SourceFileExclusionFilterConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<SourceFileExclusionFilterConfigurationItem>>() != null);
                CreateCollection();
                // ReSharper disable once AssignNullToNotNullAttribute
                return _items;
            }
        }

        public event EventHandler<PropertyChangedEventArgs> ItemPropertyChanged
        {
            add
            {
                CreateCollection();
                // ReSharper disable once PossibleNullReferenceException
                _changeTracker.ItemPropertyChanged += value;
            }
            remove
            {
                CreateCollection();
                // ReSharper disable once PossibleNullReferenceException
                _changeTracker.ItemPropertyChanged -= value;
            }
        }

        [NotNull]
        public static SourceFileExclusionFilterConfiguration Default
        {
            get
            {
                Contract.Ensures(Contract.Result<SourceFileExclusionFilterConfiguration>() != null);

                var value = new SourceFileExclusionFilterConfiguration();

                value.Add(@"Migrations\\\d{15}");

                return value;
            }
        }

        private void Add(string expression)
        {
            Items.Add(
                new SourceFileExclusionFilterConfigurationItem
                {
                    Expression = expression
                });
        }

        private void CreateCollection()
        {
            Contract.Ensures(_items != null);
            Contract.Ensures(_changeTracker != null);

            if (_items != null)
                return;

            _items = new ObservableCollection<SourceFileExclusionFilterConfigurationItem>();
            _changeTracker = new ObservablePropertyChangeTracker<SourceFileExclusionFilterConfigurationItem>(_items);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant((_items == null) || (_changeTracker != null));
        }
    }
}
