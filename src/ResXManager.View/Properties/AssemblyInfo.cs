using System.Resources;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: XmlnsPrefix("urn:ResXManager.View", "view")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View.Properties")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View.Behaviors")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View.ColumnHeaders")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View.Converters")]
[assembly: XmlnsDefinition("urn:ResXManager.View", "ResXManager.View.Visuals")]

[assembly: InternalsVisibleTo("ResXManager.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100fbfea61559d9676195b8373508daa5755fc71550caf26ae58032ea57c651e9a97b09ab548aeb7bbb311a1df8fb57283323f9ea09aaa6751b5ac0ae5232684296ebc433ebb08e6c935ef9dd5463ce2e741748aaa8175c54fb2d7454926b35bd739f40797fae6a8c2a520a5cd77993b163b5c6ff37f70dc3ec2ae2905f2e4776dc")]

namespace ResXManager.View.Properties;

public static class AssemblyKey
{
}