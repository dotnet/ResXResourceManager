namespace ResXManager.Tests
{
    using System.Globalization;
    using System.Linq;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using Xunit;

    public class UnitTest1
    {
        [Theory]
        [InlineData("de-DE", true)]
        [InlineData("en", true)]
        [InlineData("ee", true)]
        [InlineData("en-GB-dev", true)]
        [InlineData("ab-cd", false)]
        [InlineData("xy-ab", false)]
        [InlineData("xy-abc", false)]
        [InlineData("en-dummy", false)]
        [InlineData("some", false)]
        [InlineData("qps-ploc", true)] // pseudo locale
        public void IsValidLanguageNameTest(string cultureName, bool expected)
        {
            Assert.Equal(expected, CultureHelper.IsValidCultureName(cultureName));
        }

        [Fact]
        public void AllExistingCulturesAreValid()
        {
            foreach (var cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => !string.IsNullOrEmpty(c.ToString())))
            {
                Assert.True(CultureHelper.IsValidCultureName(cultureInfo.Name), cultureInfo.ToString());
                Assert.True(CultureHelper.IsValidCultureName(cultureInfo.IetfLanguageTag), cultureInfo.ToString());
            }
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

        [Fact]
        public void StringConcatBehavior()
        {
            const string? s1 = default;
            const string? s2 = default;
            const string s3 = "Test";

#pragma warning disable xUnit2000 // Constants and literals should be the expected argument
            Assert.Equal(string.Empty, s1 + s2);
#pragma warning restore xUnit2000 // Constants and literals should be the expected argument
            Assert.Equal(s3, s1 + s3);
            Assert.Equal(s3, s1 + s3 + s2);
        }
    }
}
