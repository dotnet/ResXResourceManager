namespace ResXManager.Tests.View.Converters;

using System;
using System.Globalization;
using System.Windows.Media.Imaging;

using NSubstitute;

using ResXManager.Model;
using ResXManager.View.Converters;
using ResXManager.View.Tools;

using Xunit;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public static class CultureToImageSourceConverterTests
{
    public class The_Convert_Method
    {
        [Theory]
        [InlineData("en", "us.gif")]
        [InlineData("fy", "fy.gif")]
        [InlineData("fy-NL", "nl.gif")]
        [InlineData("de-DE", "de.gif")]
        [InlineData("de-AT", "at.gif")]
        [InlineData("sv", "se.gif")]
        public void Should_Produce_Image_For_Exact_And_Overriden_Culture_Match(string culture, string expectedImageName)
        {
            var configurationMock = Substitute.For<IConfiguration>();
            var converter = new CultureToImageSourceConverter(configurationMock);

            var cultureImage = converter.Convert(CultureInfo.GetCultureInfo(culture)) as BitmapImage;

            Assert.NotNull(cultureImage);
            Assert.EndsWith(expectedImageName, cultureImage.UriSource.OriginalString, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("", "", "fr", "fr.gif")]
        [InlineData("", "", "fr-CD", "cd.gif")]
        [InlineData("ar", "ar-SA", "ar", "sa.gif")]
        [InlineData("ms", "ms-BN", "ms-BN", "bn.gif")]
        public void Should_Produce_Overriden_Culture_Image_If_Overriden_With_Settings(string cultureFrom, string cultureTo,
            string culture, string expectedImageName)
        {
            var configurationMock = Substitute.For<IConfiguration>();
            if (!string.IsNullOrEmpty(cultureFrom) && !string.IsNullOrEmpty(cultureTo) )
            {
                NeutralCultureCountryOverrides.Default[CultureInfo.GetCultureInfo(cultureFrom)] = CultureInfo.GetCultureInfo(cultureTo);
            }

            var converter = new CultureToImageSourceConverter(configurationMock);
            var cultureImage = converter.Convert(CultureInfo.GetCultureInfo(culture)) as BitmapImage;

            Assert.NotNull(cultureImage);
            Assert.EndsWith(expectedImageName, cultureImage.UriSource.OriginalString, StringComparison.OrdinalIgnoreCase);
        }
    }
}