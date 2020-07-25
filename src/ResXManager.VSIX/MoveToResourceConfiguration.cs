namespace ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;

    using ResXManager.Model;

    [DataContract]
    public sealed class MoveToResourceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember]
        public string? Extensions { get; set; }

        [DataMember]
        public string? Patterns { get; set; }

        public IEnumerable<string> ParseExtensions()
        {
            if (Extensions == null || string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        public IEnumerable<string> ParsePatterns()
        {
            if (Patterns == null || string.IsNullOrEmpty(Patterns))
                return Enumerable.Empty<string>();

            return Patterns.Split('|')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [KnownType(typeof(MoveToResourceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<MoveToResourceConfiguration>))]
    public class MoveToResourceConfiguration : ItemTrackingCollectionHost<MoveToResourceConfigurationItem>
    {
        public const string Default = @"{""Items"":
[{""Extensions"":"".cs,.vb"",""Patterns"":""$Namespace.$File.$Key|$File.$Key|StringResourceKey.$Key|$Namespace.StringResourceKey.$Key|nameof($File.$Key), ResourceType = typeof($File)|ErrorMessageResourceType = typeof($File), ErrorMessageResourceName = nameof($File.$Key)""}
,{""Extensions"":"".cshtml,.vbhtml"",""Patterns"":""@$Namespace.$File.$Key|@$File.$Key|@StringResourceKey.$Key|@$Namespace.StringResourceKey.$Key""}
,{""Extensions"":"".cpp,.c,.hxx,.h"",""Patterns"":""$File::$Key""}
,{""Extensions"":"".aspx,.ascx"",""Patterns"":""<%$ Resources:$File,$Key %>|<%= $File.$Key %>|<%= $Namespace.$File.$Key %>""}
,{""Extensions"":"".xaml"",""Patterns"":""\""{x:Static properties:$File.$Key}\""""}
,{""Extensions"":"".ts"",""Patterns"":""resources.$Key""}
,{""Extensions"":"".html"",""Patterns"":""{{ resources.$Key }}""}
]}";
    }
}
