namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Properties;

    public class UpdateNeutralColumnHeadersBehavior : Behavior<DataGrid>
    {
        private static readonly string NeutralResourceLanguagePropertyName = PropertySupport.ExtractPropertyName(() => Settings.Default.NeutralResourceLanguage);
        protected override void OnAttached()
        {
            base.OnAttached();

            Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Settings.Default.PropertyChanged -= Settings_PropertyChanged;
        }

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AssociatedObject == null)
                return;
            if (e.PropertyName != NeutralResourceLanguagePropertyName)
                return;

            UpdateNeutralColumnHeader<LanguageHeader>();
            UpdateNeutralColumnHeader<CommentHeader>();
        }

        private void UpdateNeutralColumnHeader<T>()
            where T : class, ILanguageColumnHeader, new()
        {
            Contract.Requires(AssociatedObject != null);

            var neutralLanguageColumn = AssociatedObject.Columns
                .Select(col => new {Column = col, Header = col.Header as T})
                .Where(item => (item.Header != null) && (item.Header.CultureKey == null))
                .Select(item => item.Column)
                .FirstOrDefault();

            if (neutralLanguageColumn != null)
            {
                neutralLanguageColumn.Header = new T();
            }
        }
    }
}
