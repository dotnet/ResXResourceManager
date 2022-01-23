using System.Resources;
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

namespace ResXManager.View.Properties;

public static class AssemblyKey
{
}

