namespace tomenglertde.ResXManager.VSIX
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;

    [DataContract]
    public sealed class MoveToResourceConfigurationItem : INotifyPropertyChanged
    {
        [DataMember, CanBeNull]
        public string Extensions { get; set; }

        [DataMember, CanBeNull]
        public string Patterns { get; set; }

        [NotNull, ItemNotNull]
        public IEnumerable<string> ParseExtensions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            if (string.IsNullOrEmpty(Extensions))
                return Enumerable.Empty<string>();

            return Extensions.Split(',')
                .Select(ext => ext.Trim())
                .Where(ext => !string.IsNullOrEmpty(ext));
        }

        [NotNull, ItemNotNull]
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
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            Contract.Requires(propertyName != null);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    [KnownType(typeof(MoveToResourceConfigurationItem))]
    [DataContract]
    [TypeConverter(typeof(JsonSerializerTypeConverter<MoveToResourceConfiguration>))]
    [UsedImplicitly]
    public class MoveToResourceConfiguration : ItemTrackingCollectionHost<MoveToResourceConfigurationItem>
    {
        public const string Default = @"{""Items"":
[{""Extensions"":"".cs,.vb"",""Patterns"":""$Namespace.$File.$Key|$File.$Key|StringResourceKey.$Key|$Namespace.StringResourceKey.$Key|nameof($File.$Key), ResourceType = typeof($File)|ErrorMessageResourceType = typeof($File), ErrorMessageResourceName = nameof($File.$Key)""}
,{""Extensions"":"".cshtml,.vbhtml"",""Patterns"":""@$Namespace.$File.$Key|@$File.$Key|@StringResourceKey.$Key|@$Namespace.StringResourceKey.$Key""}
,{""Extensions"":"".cpp,.c,.hxx,.h"",""Patterns"":""$File::$Key""}
,{""Extensions"":"".aspx,.ascx"",""Patterns"":""<%$ Resources:$File,$Key %>|<%= $File.$Key >|<%= $Namespace.$File.$Key %>""}
,{""Extensions"":"".xaml"",""Patterns"":""\""{x:Static properties:$File.$Key}\""""}]}";
    }
}
