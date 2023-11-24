namespace ResXManager.Tests.Model;

using System.Globalization;
using System.Linq;

using ResXManager.Model;

using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1034 // Nested types should not be visible

public static class RecentFolderConfigurationTests
{
    public sealed class The_Add_Method
    {
        [Fact]
        public void Should_Not_Contain_Duplicates()
        {
            var configuration = new RecentFolderConfiguration();
            for (int i = 0; i < 10; i++)
            {
                configuration.Add(i.ToString(CultureInfo.InvariantCulture));
            }

            configuration.Add("5");

            Assert.Equal(10, configuration.Items.Select(item => item.Folder).Distinct().Count());
        }

        [Theory]
        [InlineData("20")]
        [InlineData("2")]
        public void Should_Have_The_Most_Recent_On_Top(string newValue)
        {
            var configuration = new RecentFolderConfiguration();
            for (int i = 0; i < 10; i++)
            {
                configuration.Add(i.ToString(CultureInfo.InvariantCulture));
            }

            configuration.Add(newValue);

            Assert.Equal(newValue, configuration.Items[0].Folder);
        }

        [Theory]
        [InlineData("20", "0")]
        [InlineData("2", "")]
        public void Should_Remove_Items_To_Keep_Total_Less_Or_Equal_Than_10(string newValue, string shouldNotContainValue)
        {
            var configuration = new RecentFolderConfiguration();
            for (int i = 0; i < 10; i++)
            {
                configuration.Add(i.ToString(CultureInfo.InvariantCulture));
            }

            configuration.Add(newValue);

            Assert.DoesNotContain(shouldNotContainValue, configuration.Items.Select(item => item.Folder));
        }
    }
}