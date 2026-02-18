namespace ResXManager.Tests.View.Converters;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using NSubstitute;

using ResXManager.Model;
using ResXManager.View.Converters;
using ResXManager.View.Tools;

using VerifyXunit;

using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

public static class CultureToImageSourceConverterTests
{
    public sealed class The_Convert_Method
    {
        private readonly IConfiguration _configurationMock = Substitute.For<IConfiguration>();
        private CultureCountryOverrides? _cultureCountryOverrides;
        private CultureToImageSourceConverter? _converter;

        private CultureCountryOverrides CultureCountryOverrides => _cultureCountryOverrides ??= new CultureCountryOverrides(_configurationMock);

        private CultureToImageSourceConverter Converter => _converter ??= new CultureToImageSourceConverter(_configurationMock, CultureCountryOverrides);

        [Fact]
        public async Task Should_Produce_Correct_Default_Image_For_Every_Culture()
        {
            var images = CultureInfo
                .GetCultures(CultureTypes.AllCultures)
                .Where(culture => culture.LCID != 127)
                .Select(culture => new { Culture = culture, Image = GetImageFileName(culture) })
                .Select(value => $"{value.Culture} => {value.Image}")
                .ToArray();

            await Verifier.Verify(string.Join("\n", images));
        }

        [Theory]
        [InlineData("en", "us.gif")]
        [InlineData("fy", "fy.gif")]
        [InlineData("fy-NL", "nl.gif")]
        [InlineData("de-DE", "de.gif")]
        [InlineData("de-AT", "at.gif")]
        [InlineData("sv", "se.gif")]
        [InlineData("ca-ES", "es.gif")]
        [InlineData("ca-ES-valencia", "es.gif")]
        [InlineData("sr", "rs.gif")]
        [InlineData("sr-Cyrl", "rs.gif")]
        [InlineData("sr-Latn", "rs.gif")]
        public void Should_Use_Region_As_Primary_Source_For_Flags(string culture, string expectedImageName)
        {
            var imageName = GetImageFileName(CultureInfo.GetCultureInfo(culture));

            Assert.NotNull(imageName);
            Assert.Equal(expectedImageName, imageName, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("ar=ar-SA", "ar", "sa.gif")]
        [InlineData("ms=ms-BN", "ms-BN", "bn.gif")]
        // Overriding a specific language does not override parent
        [InlineData("ca-ES-valencia=sa", "ca-ES", "es.gif")]
        // It's possible to override specific culture if s.o. does not like the defaults
        [InlineData("ca-ES-valencia=sa", "ca-ES-valencia", "sa.gif")]
        [InlineData("fy-NL=fy", "fy-NL", "fy.gif")]
        public void Should_Produce_Overriden_Culture_Image_If_Overriden_With_Settings(string overrides, string culture, string expectedImageName)
        {
            _configurationMock.CultureCountyOverrides = overrides;

            var imageName = GetImageFileName(CultureInfo.GetCultureInfo(culture));

            Assert.NotNull(imageName);
            Assert.Equal(expectedImageName, imageName, StringComparer.OrdinalIgnoreCase);
        }

        private string? GetImageFileName(CultureInfo culture)
        {
            var image = Converter.Convert(culture) as BitmapImage;
            var fileName = Path.GetFileName(image?.UriSource.ToString());

            return fileName;
        }
    }
}
