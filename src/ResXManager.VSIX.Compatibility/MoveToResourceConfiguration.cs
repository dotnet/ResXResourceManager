namespace ResXManager.VSIX.Compatibility;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

using ResXManager.Model;

using TomsToolbox.Essentials;

[DataContract]
public sealed class MoveToResourceConfigurationItem : INotifyPropertyChanged
{
    [DataMember]
    public string? Extensions { get; set; }

    [DataMember]
    public string? Patterns { get; set; }

    public IEnumerable<string> ParseExtensions()
    {
        if (Extensions == null || Extensions.IsNullOrEmpty())
            return Enumerable.Empty<string>();

        return Extensions.Split(',')
            .Select(ext => ext.Trim())
            .Where(ext => !ext.IsNullOrEmpty());
    }

    public IEnumerable<string> ParsePatterns()
    {
        if (Patterns == null || Patterns.IsNullOrEmpty())
            return Enumerable.Empty<string>();

        return Patterns.Split('|')
            .Select(ext => ext.Trim())
            .Where(ext => !ext.IsNullOrEmpty());
    }

#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
}

[KnownType(typeof(MoveToResourceConfigurationItem))]
[DataContract]
[TypeConverter(typeof(JsonSerializerTypeConverter<MoveToResourceConfiguration>))]
public class MoveToResourceConfiguration : ItemTrackingCollectionHost<MoveToResourceConfigurationItem>
{
    public const string Default = @"{""Items"":
[{""Extensions"":"".cs,.vb"",""Patterns"":""$Namespace.$File.$Key|$File.$Key|StringResourceKey.$Key|$Namespace.StringResourceKey.$Key|nameof($File.$Key), ResourceType = typeof($File)|ErrorMessageResourceType = typeof($File), ErrorMessageResourceName = nameof($File.$Key)""}
,{""Extensions"":"".cshtml,.vbhtml,.razor"",""Patterns"":""@$Namespace.$File.$Key|@$File.$Key|@StringResourceKey.$Key|@$Namespace.StringResourceKey.$Key""}
,{""Extensions"":"".cpp,.c,.hxx,.h"",""Patterns"":""$File::$Key""}
,{""Extensions"":"".aspx,.ascx"",""Patterns"":""<%$ Resources:$File,$Key %>|<%= $File.$Key %>|<%= $Namespace.$File.$Key %>""}
,{""Extensions"":"".xaml"",""Patterns"":""\""{x:Static properties:$File.$Key}\""""}
,{""Extensions"":"".ts"",""Patterns"":""resources.$Key""}
,{""Extensions"":"".html"",""Patterns"":""{{ resources.$Key }}""}
]}";
}
