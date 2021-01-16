namespace ResXManager.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;

    using Newtonsoft.Json;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;

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
            Assert.Equal(expected, Model.ResourceManager.IsValidLanguageName(cultureName));
        }
    }
}
