namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Controls;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for Translations.xaml
    /// </summary>
    [DataTemplate(typeof(TranslationsViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class TranslationsView
    {
        public TranslationsView()
        {
            InitializeComponent();
        }

        private void ComboBox_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!true.Equals(e.NewValue))
                return;

            var element = sender as DependencyObject;
            if (element == null)
                return;

            var row = element.TryFindAncestor<DataGridRow>();
            if (row != null)
            {
                row.IsSelected = true;
            }
        }
    }
}
