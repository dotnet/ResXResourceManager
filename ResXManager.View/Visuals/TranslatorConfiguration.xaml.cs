namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    /// <summary>
    ///     Interaction logic for TranslatorConfiguration.xaml
    /// </summary>
    public partial class TranslatorConfiguration
    {
        public TranslatorConfiguration()
        {
            InitializeComponent();
        }

        [ItemNotNull]
        [CanBeNull]
        public IEnumerable<ITranslator> Translators
        {
            get => (IEnumerable<ITranslator>)GetValue(TranslatorsProperty);
            set => SetValue(TranslatorsProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="Translators"/> dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty TranslatorsProperty =
            DependencyProperty.Register("Translators", typeof(IEnumerable<ITranslator>), typeof(TranslatorConfiguration));

        private void TabControl_Loaded([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            var tabControl = (TabControl)sender;
            tabControl.SelectedIndex = 0;
        }
    }
}