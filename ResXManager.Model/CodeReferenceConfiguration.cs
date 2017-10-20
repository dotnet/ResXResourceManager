namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    [DataContract]
    public sealed class CodeReferenceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember, CanBeNull]
        public string Extensions { get; set; }

        [DataMember]
        public bool IsCaseSensitive { get; set; }

        [DataMember, CanBeNull]
        public string Expression { get; set; }

        [DataMember, CanBeNull]
        public string SingleLineComment { get; set; }

        [NotNull, ItemNotNull]
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
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }


    [KnownType(typeof(CodeReferenceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<CodeReferenceConfiguration>))]
    [UsedImplicitly]
    public sealed class CodeReferenceConfiguration : ItemTrackingCollectionHost<CodeReferenceConfigurationItem>
    {
        public const string Default = @"{""Items"":[
{""Expression"":""\\W($File.$Key)\\W"",""Extensions"":"".cs,.xaml,.cshtml"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""ResourceManager.GetString\\(\""($Key)\""\\)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""typeof\\((\\w+\\.)*($File)\\).+\""($Key)\""|\""($Key)\"".+typeof\\((\\w+\\.)*($File)\\)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""\\W($Key)\\W"",""Extensions"":"".vb"",""IsCaseSensitive"":false,""SingleLineComment"":""'""},
{""Expression"":""\\W($File::$Key)\\W"",""Extensions"":"".cpp,.c,.hxx,.h"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""&lt;%\\$\\s+Resources:\\s*($File)\\s*,\\s*($Key)\\s*%&gt;"",""Extensions"":"".aspx,.ascx,.master"",""IsCaseSensitive"":true,""SingleLineComment"":null},
{""Expression"":""StringResourceKey\\.($Key)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""}]}";
    }
}
