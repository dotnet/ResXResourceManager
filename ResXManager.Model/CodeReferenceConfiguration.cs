using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using tomenglertde.ResXManager.Model;

namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;

    [DataContract]
    public class CodeReferenceConfigurationItem
    {
        [DataMember]
        public string Extensions
        {
            get;
            set;
        }

        [DataMember]
        public bool IsCaseSensitive
        {
            get;
            set;
        }

        [DataMember]
        public string Expression
        {
            get;
            set;
        }

        [DataMember]
        public string SingleLineComment
        {
            get;
            set;
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
    }

    [KnownType(typeof(CodeReferenceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<CodeReferenceConfiguration>))]
    public class CodeReferenceConfiguration
    {
        private ObservableCollection<CodeReferenceConfigurationItem> _items = new ObservableCollection<CodeReferenceConfigurationItem>();

        [DataMember(Name = "Items")]
        public ObservableCollection<CodeReferenceConfigurationItem> Items
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CodeReferenceConfigurationItem>>() != null);
                return _items;
            }
            set
            {
                Contract.Requires(value != null);
                _items = value;
            }
        }

        public static CodeReferenceConfiguration Default
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                var value = new CodeReferenceConfiguration();

                value.Add(".cs,.xaml", true, @"\W($File.$Key)\W", "//");
                value.Add(".cs", true, "ResourceManager.GetString\\(\"($Key)\"\\)", "//");
                value.Add(".cs", true, "typeof\\(($File)\\).+\"($Key)\"|\"($Key)\".+typeof\\(($File)\\)", "//");
                value.Add(".vb", false, @"\W($Key)\W", "'");
                value.Add(".cpp,.c,.hxx,.h", true, @"\W($File::$Key)\W", "//");
                value.Add(".cshtml;.aspx;.ascx", true, @"<%\$\s+Resources:\s*($File)\s*,\s*($Key)\s*%>", null);

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

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_items != null);
        }
    }
}
