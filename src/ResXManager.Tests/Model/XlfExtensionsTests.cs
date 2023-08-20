namespace ResXManager.Model.Tests;

using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using VerifyXunit;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

public static class XlfExtensionsTests
{
    private static void SetElementNamespaces(XElement element)
    {
        element.Name = XlfNames.XliffNS + element.Name.LocalName;

        foreach (var childElement in element.Elements())
        {
            SetElementNamespaces(childElement);
        }
    }

    [UsesVerify]
    public class The_SetNoteValue_Method
    {
        [Fact]
        public async Task Correctly_Handles_Setting_Notes_To_Valid_Value()
        {
            const string TestData = @"<trans-unit id=""MyProject.MyResource"" translate=""yes"" xml:space=""preserve"">
  <source>Last time I did something fun</source>
  <target state=""translated"">De laatste keer dat ik iets leuks deed</target>
  <note from=""resx"">Choice will be shown after this</note>
  <note from=""MultilingualBuild"" annotates=""source"" priority=""2"">Choice will be shown after this</note>
</trans-unit>";

            var element = XElement.Parse(TestData);

            SetElementNamespaces(element);

            XlfExtensions.SetNoteValue(element, XlfNames.FromResx, "test1");
            XlfExtensions.SetNoteValue(element, XlfNames.FromResxSpecific, "test2");

            var output = element.ToString(SaveOptions.None);

            await Verifier.Verify(output);
        }

        [Fact]
        public async Task Correctly_Handles_Setting_Notes_To_Null_Value()
        {
            const string TestData = @"<trans-unit id=""MyProject.MyResource"" translate=""yes"" xml:space=""preserve"">
  <source>Last time I did something fun</source>
  <target state=""translated"">De laatste keer dat ik iets leuks deed</target>
  <note from=""resx"">Choice will be shown after this</note>
  <note from=""MultilingualBuild"" annotates=""source"" priority=""2"">Choice will be shown after this</note>
</trans-unit>";

            var element = XElement.Parse(TestData);

            SetElementNamespaces(element);

            XlfExtensions.SetNoteValue(element, XlfNames.FromResx, null);
            XlfExtensions.SetNoteValue(element, XlfNames.FromResxSpecific, null);

            var output = element.ToString(SaveOptions.None);

            await Verifier.Verify(output);
        }
    }
}