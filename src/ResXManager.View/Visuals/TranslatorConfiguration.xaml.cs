namespace ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    using ResXManager.Infrastructure;

    /// <summary>
    /// Interaction logic for TranslatorConfiguration.xaml
    /// </summary>
    public partial class TranslatorConfiguration
    {
        public TranslatorConfiguration()
        {
            InitializeComponent();
        }

        public IEnumerable<ITranslator>? Translators
        {
            get => (IEnumerable<ITranslator>)GetValue(TranslatorsProperty);
            set => SetValue(TranslatorsProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="Translators"/> dependency property
        /// </summary>
        public static readonly DependencyProperty TranslatorsProperty =
            DependencyProperty.Register("Translators", typeof(IEnumerable<ITranslator>), typeof(TranslatorConfiguration));

        private void TabControl_Loaded(object? sender, RoutedEventArgs e)
        {
            if (sender is TabControl tabControl)
            {
                tabControl.SelectedIndex = 0;
            }
        }
    }
}