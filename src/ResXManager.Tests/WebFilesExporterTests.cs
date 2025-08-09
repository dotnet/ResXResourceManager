using ResXManager.Model;

using Xunit;

namespace ResXManager.Tests;

public class WebFilesExporterTests
{
    [Theory]
    [InlineData("Hello ${name}", new[] { "name" })]
    [InlineData("Welcome {{user}}!", new[] { "user" })]
    [InlineData("No placeholders here", new string[0])]
    [InlineData("Multiple: ${first} and {{second}}", new[] { "first", "second" })]
    [InlineData("Multiple /w space: ${ first } and {{ second }}", new[] { "first", "second" })]
    [InlineData("Multiple too many space: ${  first } and {{ second  }}", new string[0])]
    [InlineData("${a}${b}${a}", new[] { "a", "b" })]
    [InlineData("Mix: ${x} {{y}} ${z}", new[] { "x", "y", "z" })]
    [InlineData("Edge: ${ } {{ }}", new string[0])]
    [InlineData("Nested: ${outer${inner}}", new[] { "inner" })] // Only matches 'inner' due to regex
    public void ExtractPlaceholders_ReturnsExpectedPlaceholders(string input, string[] expected)
    {
        // Act
        var result = WebFilesExporter.ExtractPlaceholders(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
