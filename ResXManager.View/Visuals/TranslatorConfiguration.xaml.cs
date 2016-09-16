namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators;

    /// <summary>
    ///     Interaction logic for TranslatorConfiguration.xaml
    /// </summary>
    public partial class TranslatorConfiguration
    {
        public TranslatorConfiguration()
        {
            InitializeComponent();
        }

        public IEnumerable<ITranslator> Translators
        {
            get { return (IEnumerable<ITranslator>)GetValue(TranslatorsProperty); }
            set { SetValue(TranslatorsProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Translators"/> dependency property
        /// </summary>
        public static readonly DependencyProperty TranslatorsProperty =
            DependencyProperty.Register("Translators", typeof (IEnumerable<ITranslator>), typeof (TranslatorConfiguration));

        private void TabControl_Loaded(object sender, RoutedEventArgs e)
        {
            Contract.Requires(sender != null);

            var tabControl = (TabControl)sender;
            tabControl.SelectedIndex = 0;
        }
    }
}