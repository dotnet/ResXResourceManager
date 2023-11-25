namespace ResXManager.Tests.Model;

using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

using static Properties.Settings;

public static class RecentFolderConfigurationTests
{
    public sealed class The_Add_Method
    {
        [Fact]
        public void Should_Not_Contain_Duplicates()
        {
            var items = new StringCollection();

            for (var i = 0; i < 10; i++)
            {
                items = AddStartupFolder(items, i.ToString(CultureInfo.InvariantCulture));
            }

            items = AddStartupFolder(items, "5");

            Assert.Equal(10, items.Cast<string>().Distinct().Count());
        }

        [Theory]
        [InlineData("20")]
        [InlineData("2")]
        public void Should_Have_The_Most_Recent_On_Top(string newValue)
        {
            var items = new StringCollection();

            for (var i = 0; i < 10; i++)
            {
                items = AddStartupFolder(items, i.ToString(CultureInfo.InvariantCulture));
            }

            items = AddStartupFolder(items, newValue);

            Assert.Equal(newValue, items[0]);
        }

        [Theory]
        [InlineData("20", "0")]
        [InlineData("2", "")]
        public void Should_Remove_Items_To_Keep_Total_Less_Or_Equal_Than_10(string newValue, string shouldNotContainValue)
        {
            var items = new StringCollection();

            for (var i = 0; i < 10; i++)
            {
                items = AddStartupFolder(items, i.ToString(CultureInfo.InvariantCulture));
            }

            items = AddStartupFolder(items, newValue);

            Assert.DoesNotContain(shouldNotContainValue, items.Cast<string>());
        }
    }
}