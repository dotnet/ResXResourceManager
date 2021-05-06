namespace ResXManager.Tests
{
    using ResXManager.Model;

    using Xunit;

    public class UnitTest1
    {
        [Theory]
        [InlineData("de-DE", true)]
        [InlineData("en", true)]
        [InlineData("en-GB-dev", true)]
        [InlineData("ab-cd", false)]
        [InlineData("xy-ab", false)]
        [InlineData("xy-abc", false)]
        [InlineData("en-dummy", false)]
        [InlineData("some", false)]
        [InlineData("qps-ploc", true)] // pseudo locale
        public void IsValidLanguageNameTest(string cultureName, bool expected)
        {
            Assert.Equal(expected, ResourceManager.IsValidLanguageName(cultureName));
        }

        [Theory]
        [InlineData("{0}", new[] { "{0}" }, true)]
        [InlineData("{0}", new[] { "{1}" }, false)]
        [InlineData("{0:yyyy-MM-dd}", new[] { "{0:yyyy-MM-dd}" }, true)]
        [InlineData("{0:yyyy-MM-dd}", new[] { "{0:dd-MM-yyyy}" }, true)]
        [InlineData("{0:yyyy-MM-dd}", new[] { "{1:yyyy-MM-dd}" }, false)]
        [InlineData("{0:yyyy-MM-dd}, {1}", new[] { "{0:yyyy-MM-dd} {1}" }, true)]
        [InlineData("{0:yyyy-MM-dd} {1}", new[] { "{0:yyyy-MM-dd}-{1}" }, true)]
        [InlineData("${Test}", new[] { "${Test}" }, true)]
        [InlineData("${Test} ${ T1 }", new[] { "${T1} ${Test}" }, true)]
        [InlineData("${Test} ${ T.1 }", new[] { "${T.1} ${Test}" }, true)]
        [InlineData("${Test} ${ T 1 }", new[] { "${T 2} ${Test}" }, true)]
        [InlineData("${0Test} ${ T1 }", new[] { "${T1} ${0Test}" }, true)]
        [InlineData("${Test} ${ T1 }", new[] { "${T2} ${Test}" }, false)]
        public void ResourceTableEntryRuleStringFormatTest(string neutral, string[] specific, bool expected, string? expectedMessage = null)
        {
            ResourceTableEntryRuleTest<ResourceTableEntryRuleStringFormat>(neutral, specific, expected, expectedMessage);
        }

        private static void ResourceTableEntryRuleTest<T>(string neutral, string[] specific, bool expected, string? expectedMessage = null) where T : ResourceTableEntryRule, new()
        {
            var rule = new T();

            var result = rule.CompliesToRule(neutral, specific, out var message);

            Assert.Equal(expected, result);
            if (expectedMessage != null)
            {
                Assert.Equal(expectedMessage, message);
            }
        }
    }
}
