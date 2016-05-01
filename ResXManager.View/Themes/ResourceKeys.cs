namespace tomenglertde.ResXManager.View.Themes
{
    using System.Windows;
    using System.Windows.Controls;

    public static class ResourceKeys
    {
        public static readonly ResourceKey LabelTextStyle = new ComponentResourceKey(typeof(ResourceKeys), "LabelTextStyle");

        /// <summary>
        /// A style for text boxes that automatically sets the tool tip if the text is trimmed.
        /// </summary>
        public static readonly ResourceKey AutoToolTipTextBoxStyle = new ComponentResourceKey(typeof(ResourceKeys), "AutoToolTipTextBoxStyle");

        /// <summary>
        /// Resource key for a style applying a shared with to associated container controls.
        /// </summary>
        public static readonly ResourceKey SharedWidthContainerStyle = new ComponentResourceKey(typeof(ResourceKeys), "SharedWidthContainerStyle");

        /// <summary>
        /// Data grid with check boxes for row selection: Style to be applied to the check box inside the row header template.
        /// </summary>
        public static readonly ResourceKey DataGridRowCheckBoxStyle = new ComponentResourceKey(typeof(ResourceKeys), "DataGridRowCheckBoxStyle");

        /// <summary>
        /// A control that shows validation errors in the tool tip.
        /// </summary>
        public static readonly ResourceKey ControlWithValidationErrorToolTipStyle = new ComponentResourceKey(typeof(ResourceKeys), "ControlWithValidationErrorToolTipStyle");

        /// <summary>
        /// Template for the filter on a column represented by a DataGridTextColumn.
        /// </summary>
        public static readonly ResourceKey TextColumnFilterTemplateKey = new ComponentResourceKey(typeof(ResourceKeys), typeof(DataGridTextColumn));

        /// <summary>
        /// Template for the filter on a column represented by a DataGridCheckBoxColumn.
        /// </summary>
        public static readonly ResourceKey CheckBoxColumnFilterTemplateKey = new ComponentResourceKey(typeof(ResourceKeys), typeof(DataGridCheckBoxColumn));

        /// <summary>
        /// Template for the filter on a column represented by a DataGridCheckBoxColumn.
        /// </summary>
        public static readonly ResourceKey TemplateColumnFilterTemplateKey = new ComponentResourceKey(typeof(ResourceKeys), typeof(DataGridTemplateColumn));

        /// <summary>
        /// Template for the whole column header.
        /// </summary>
        public static readonly ResourceKey ColumnHeaderTemplateKey = new ComponentResourceKey(typeof(ResourceKeys), typeof(DataGridColumn));

        /// <summary>
        /// The filter icon template.
        /// </summary>
        public static readonly ResourceKey IconTemplateKey = new ComponentResourceKey(typeof(ResourceKeys), "IconTemplate");

        /// <summary>
        /// The filter icon style.
        /// </summary>
        public static readonly ResourceKey IconStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "IconStyle");

        /// <summary>
        /// Style for the filter check box in a filtered DataGridCheckBoxColumn.
        /// </summary>
        public static readonly ResourceKey ColumnHeaderSearchCheckBoxStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderSearchCheckBoxStyle");

        /// <summary>
        /// Style for the filter text box in a filtered DataGridTextColumn.
        /// </summary>
        public static readonly ResourceKey ColumnHeaderSearchTextBoxStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderSearchTextBoxStyle");

        /// <summary>
        /// Style for the clear button in the filter text box in a filtered DataGridTextColumn.
        /// </summary>
        public static readonly ResourceKey ColumnHeaderSearchTextBoxClearButtonStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderSearchTextBoxClearButtonStyle");


        public static readonly ResourceKey ColumnHeaderGripperToolTipStyleKey = new ComponentResourceKey(typeof(ResourceKeys), "ColumnHeaderGripperToolTipStyle");
    }
}
