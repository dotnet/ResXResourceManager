namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Windows.Controls;

    using JetBrains.Annotations;

    public class ColumnHeader : ContentControl, IColumnHeader
    {
        public ColumnHeader([CanBeNull] object content, ColumnType columnType)
        {
            Content = content;
            ColumnType = columnType;
            Focusable = false;
        }

        public ColumnType ColumnType
        {
            get;
        }
    }
}
