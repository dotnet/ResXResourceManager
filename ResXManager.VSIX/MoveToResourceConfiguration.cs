namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.ObservableCollections;

    [DataContract]
    public sealed class MoveToResourceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember]
        public string Extensions { get; set; }

        [DataMember]
        public string Patterns { get; set; }

        [NotNull]
        public IEnumerable<string> ParseExtensions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        [NotNull]
        public IEnumerable<string> ParsePatterns()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Patterns))
                return Enumerable.Empty<string>();

            return Patterns.Split('|')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator, UsedImplicitly]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    [KnownType(typeof(MoveToResourceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<MoveToResourceConfiguration>))]
    public class MoveToResourceConfiguration
    {
        private ObservableCollection<MoveToResourceConfigurationItem> _items;
        private ObservablePropertyChangeTracker<MoveToResourceConfigurationItem> _changeTracker;

        [DataMember(Name = @"Items")]
        [NotNull]
        public ObservableCollection<MoveToResourceConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<MoveToResourceConfigurationItem>>() != null);
                CreateCollection();
                // ReSharper disable once AssignNullToNotNullAttribute => CreateCollection ensures _items != null
                return _items;
            }
        }

        public event EventHandler<PropertyChangedEventArgs> ItemPropertyChanged
        {
            add
            {
                CreateCollection();
                // ReSharper disable once PossibleNullReferenceException => CreateCollection ensures _changeTracker != null
                _changeTracker.ItemPropertyChanged += value;
            }
            remove
            {
                CreateCollection();
                // ReSharper disable once PossibleNullReferenceException => CreateCollection ensures _changeTracker != null
                _changeTracker.ItemPropertyChanged -= value;
            }
        }

        [NotNull]
        public static MoveToResourceConfiguration Default
        {
            get
            {
                Contract.Ensures(Contract.Result<MoveToResourceConfiguration>() != null);

                var value = new MoveToResourceConfiguration();

                value.Add(@".cs,.vb", @"$Namespace.$File.$Key|$File.$Key|StringResourceKey.$Key|$Namespace.StringResourceKey.$Key");
                value.Add(@".cshtml,.vbhtml", @"@$Namespace.$File.$Key|@$File.$Key|@StringResourceKey.$Key|@$Namespace.StringResourceKey.$Key");
                value.Add(@".cpp,.c,.hxx,.h", @"$File::$Key");
                value.Add(@".aspx,.ascx", @"<%$ Resources:$File,$Key %>");
                value.Add(@".xaml", @"""{x:Static properties:$File.$Key}""");

                return value;
            }
        }

        private void Add(string extensions, string pattern)
        {
            Items.Add(new MoveToResourceConfigurationItem
            {
                Extensions = extensions,
                Patterns = pattern,
            });
        }

        private void CreateCollection()
        {
            Contract.Ensures(_items != null);
            Contract.Ensures(_changeTracker != null);

            if (_items != null)
                return;

            _items = new ObservableCollection<MoveToResourceConfigurationItem>();
            _changeTracker = new ObservablePropertyChangeTracker<MoveToResourceConfigurationItem>(_items);
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
