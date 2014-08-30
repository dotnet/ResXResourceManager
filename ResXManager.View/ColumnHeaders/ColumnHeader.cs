namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Windows.Controls;

    public class ColumnHeader : ContentControl, IColumnHeader
    {
        public ColumnHeader(object content, ColumnType columnType)
        {
            Content = content;
            ColumnType = columnType;
        }

        public ColumnType ColumnType
        {
            get;
            private set;
        }
    }
}
