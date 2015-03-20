namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Windows;
    using System.Windows.Controls.Primitives;

    using tomenglertde.ResXManager.Translators;

    using TomsToolbox.Desktop;

    /// <summary>
    /// Interaction logic for TranslatorConfiguration.xaml
    /// </summary>
    public partial class TranslatorConfiguration
    {
        private readonly Throttle _changeThrottle = new Throttle(TimeSpan.FromSeconds(1), TranslatorHost.SaveConfiguration);

        public TranslatorConfiguration()
        {
            InitializeComponent();

            EventManager.RegisterClassHandler(typeof(TranslatorConfiguration), ButtonBase.ClickEvent, new RoutedEventHandler(Element_Changed));
            EventManager.RegisterClassHandler(typeof(TranslatorConfiguration), TextBoxBase.TextChangedEvent, new RoutedEventHandler(Element_Changed));
        }

        private void Element_Changed(object sender, RoutedEventArgs e)
        {
            _changeThrottle.Tick();
        }
    }
}
