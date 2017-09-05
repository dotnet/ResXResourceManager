namespace tomenglertde.ResXManager.Model
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

    using TomsToolbox.ObservableCollections;

    [DataContract]
    public sealed class CodeReferenceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember]
        public string Extensions { get; set; }

        [DataMember]
        public bool IsCaseSensitive { get; set; }

        [DataMember]
        public string Expression { get; set; }

        [DataMember]
        public string SingleLineComment { get; set; }

        [NotNull]
        public IEnumerable<string> ParseExtensions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                // ReSharper disable once PossibleNullReferenceException
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

    [KnownType(typeof(CodeReferenceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<CodeReferenceConfiguration>))]
    public class CodeReferenceConfiguration
    {
        private ObservableCollection<CodeReferenceConfigurationItem> _items;
        private ObservablePropertyChangeTracker<CodeReferenceConfigurationItem> _changeTracker;

        [DataMember(Name = "Items")]
        [NotNull]
        public ObservableCollection<CodeReferenceConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CodeReferenceConfigurationItem>>() != null);
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
        public static CodeReferenceConfiguration Default
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                var value = new CodeReferenceConfiguration();

                value.Add(".cs,.xaml,.cshtml", true, @"\W($File.$Key)\W", @"//");
                value.Add(".cs", true, @"ResourceManager.GetString\(""($Key)""\)", @"//");
                value.Add(".cs", true, @"typeof\((\w+\.)*($File)\).+""($Key)""|""($Key)"".+typeof\((\w+\.)*($File)\)", @"//");
                value.Add(".vb", false, @"\W($Key)\W", @"'");
                value.Add(".cpp,.c,.hxx,.h", true, @"\W($File::$Key)\W", @"//");
                value.Add(".aspx,.ascx", true, @"<%\$\s+Resources:\s*($File)\s*,\s*($Key)\s*%>", null);
                value.Add(".cs", true, @"StringResourceKey\.($Key)", @"//");

                return value;
            }
        }

        private void Add(string extensions, bool isCaseSensitive, string expression, string singleLineComment)
        {
            Items.Add(
                new CodeReferenceConfigurationItem
                {
                    Extensions = extensions,
                    IsCaseSensitive = isCaseSensitive,
                    Expression = expression,
                    SingleLineComment = singleLineComment
                });
        }

        private void CreateCollection()
        {
            Contract.Ensures(_items != null);
            Contract.Ensures(_changeTracker != null);

            if (_items != null)
                return;

            _items = new ObservableCollection<CodeReferenceConfigurationItem>();
            _changeTracker = new ObservablePropertyChangeTracker<CodeReferenceConfigurationItem>(_items);
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
