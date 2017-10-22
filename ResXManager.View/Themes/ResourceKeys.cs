using JetBrains.Annotations;

namespace tomenglertde.ResXManager.View.Themes
{
    using System.Windows;

    public static class ResourceKeys
    {
        /// <summary>
        /// A data template for top level menus with an image.
        /// </summary>
        [NotNull]
        public static readonly ResourceKey MenuItemDropDownDataTemplate = new ComponentResourceKey(typeof(ResourceKeys), "MenuItemDropDownDataTemplate");

        /// <summary>
        /// The filter icon style.
        /// </summary>
        [NotNull]
        public static readonly ResourceKey IconStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "IconStyle");

        /// <summary>
        /// Style for the filter text box in a filtered DataGridTextColumn.
        /// </summary>
        [NotNull]
        public static readonly ResourceKey ColumnHeaderSearchTextBoxStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderSearchTextBoxStyle");

        /// <summary>
        /// Style for the clear button in the filter text box in a filtered DataGridTextColumn.
        /// </summary>
        [NotNull]
        public static readonly ResourceKey ColumnHeaderSearchTextBoxClearButtonStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderSearchTextBoxClearButtonStyle");


        [NotNull]
        public static readonly ResourceKey ColumnHeaderGripperToolTipStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderGripperToolTipStyle");
    }
}
