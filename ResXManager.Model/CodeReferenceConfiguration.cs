namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;

    [DataContract]
    public class CodeReferenceConfigurationItem : INotifyPropertyChanged
    {
        private string _extensions;
        private bool _isCaseSensitive;
        private string _expression;
        private string _singleLineComment;

        [DataMember]
        public string Extensions
        {
            get
            {
                return _extensions;
            }
            set
            {
                SetProperty(ref _extensions, value, () => Extensions);
            }
        }

        [DataMember]
        public bool IsCaseSensitive
        {
            get
            {
                return _isCaseSensitive;
            }
            set
            {
                SetProperty(ref _isCaseSensitive, value, () => IsCaseSensitive);
            }
        }

        [DataMember]
        public string Expression
        {
            get
            {
                return _expression;
            }
            set
            {
                SetProperty(ref _expression, value, () => Expression);
            }
        }

        [DataMember]
        public string SingleLineComment
        {
            get
            {
                return _singleLineComment;
            }
            set
            {
                SetProperty(ref _singleLineComment, value, () => SingleLineComment);
            }
        }

        public IEnumerable<string> ParseExtensions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetProperty<T>(ref T backingField, T value, Expression<Func<T>> propertyExpression)
        {
            Contract.Requires(propertyExpression != null);

            SetProperty(ref backingField, value, PropertySupport.ExtractPropertyName(propertyExpression));
        }

        [NotifyPropertyChangedInvocator]
        private void SetProperty<T>(ref T backingField, T value, string propertyName)
        {
            Contract.Requires(propertyName != null);

            if (Equals(backingField, value))
                return;

            backingField = value;

            OnPropertyChanged(propertyName);
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

        public CodeReferenceConfiguration()
        {
            CreateCollection();
        }

        [DataMember(Name = "Items")]
        public ObservableCollection<CodeReferenceConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CodeReferenceConfigurationItem>>() != null);
                return _items ?? CreateCollection();
            }
        }

        public event EventHandler<PropertyChangedEventArgs> ItemPropertyChanged
        {
            add
            {
                _changeTracker.ItemPropertyChanged += value;
            }
            remove
            {
                _changeTracker.ItemPropertyChanged -= value;
            }
        }

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

        private ObservableCollection<CodeReferenceConfigurationItem> CreateCollection()
        {
            Contract.Ensures(_items != null);
            Contract.Ensures(_changeTracker != null);

            _items = new ObservableCollection<CodeReferenceConfigurationItem>();
            _changeTracker = new ObservablePropertyChangeTracker<CodeReferenceConfigurationItem>(_items);

            return _items;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_items != null);
            Contract.Invariant(_changeTracker != null);
        }
    }
}
