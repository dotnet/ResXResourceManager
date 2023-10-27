namespace ResXManager.Tests.View.Tools;

using System.Globalization;

using ResXManager.View.Tools;

using Xunit;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public static class ExactCultureOverridesTests
{
    public class The_ReadDefault_Method
    {
        [Fact]
        public void Initializes_Cultures_Correctly()
        {
            var action = () => ExactCultureOverrides.Exact.HasCustomFlag(CultureInfo.CurrentCulture);

            var exception = Record.Exception(() => action());
            Assert.Null(exception);
        }
    }
}