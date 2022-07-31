namespace ResXManager.Model
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;

    using TomsToolbox.Essentials;

    [DataContract]
    public sealed class CodeReferenceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember]
        public string? Extensions { get; set; }

        [DataMember]
        public bool IsCaseSensitive { get; set; }

        [DataMember]
        public string? Expression { get; set; }

        [DataMember]
        public string? SingleLineComment { get; set; }

        public IEnumerable<string> ParseExtensions()
        {
            if (Extensions.IsNullOrEmpty())
                return Enumerable.Empty<string>();

            return Extensions
                .Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !ext.IsNullOrEmpty());
        }

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }


    [KnownType(typeof(CodeReferenceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<CodeReferenceConfiguration>))]
    public sealed class CodeReferenceConfiguration : ItemTrackingCollectionHost<CodeReferenceConfigurationItem>
    {
        public const string Default = @"{""Items"":[
{""Expression"":""\\W($File.$Key)\\W"",""Extensions"":"".cs,.xaml,.cshtml"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""\\W($File.$Key)\\W"",""Extensions"":"".vbhtml"",""IsCaseSensitive"":false,""SingleLineComment"":null},
{""Expression"":""ResourceManager.GetString\\(\""($Key)\""\\)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""typeof\\((\\w+\\.)*($File)\\).+\""($Key)\""|\""($Key)\"".+typeof\\((\\w+\\.)*($File)\\)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""\\W($Key)\\W"",""Extensions"":"".vb"",""IsCaseSensitive"":false,""SingleLineComment"":""'""},
{""Expression"":""\\W($File::$Key)\\W"",""Extensions"":"".cpp,.c,.hxx,.h"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""&lt;%\\$\\s+Resources:\\s*($File)\\s*,\\s*($Key)\\s*%&gt;"",""Extensions"":"".aspx,.ascx,.master"",""IsCaseSensitive"":true,""SingleLineComment"":null},
{""Expression"":""StringResourceKey\\.($Key)"",""Extensions"":"".cs"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""},
{""Expression"":""\\.($Key)"",""Extensions"":"".ts,.html"",""IsCaseSensitive"":true,""SingleLineComment"":""\/\/""}
]}";
    }
}
