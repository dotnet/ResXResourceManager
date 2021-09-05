namespace ResXManager
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Composition;
    using System.Windows;
    using System.Windows.Threading;

    using PropertyChanged;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Properties;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    public enum ColorTheme
    {
        [LocalizedDisplayName(StringResourceKey.ColorTheme_System)]
        System,

        [LocalizedDisplayName(StringResourceKey.ColorTheme_Light)]
        Light,

        [LocalizedDisplayName(StringResourceKey.ColorTheme_Dark)]
        Dark
    }

    [Export(typeof(IConfiguration))]
    [Shared]
    public class StandaloneConfiguration : Configuration
    {
        private readonly Collection<ResourceDictionary> _colorThemeResourceContainer;

        [ImportingConstructor]
        public StandaloneConfiguration(ITracer tracer)
            : base(tracer)
        {
            var themeDictionary = new ResourceDictionary();
            var applicationDictionaries = Application.Current.Resources.MergedDictionaries;
            applicationDictionaries.Insert(0, themeDictionary);
            _colorThemeResourceContainer = themeDictionary.MergedDictionaries;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, OnColorThemeChanged);
        }

        public override bool IsScopeSupported => false;

        public override ConfigurationScope Scope => ConfigurationScope.Global;

        [DefaultValue(nameof(ColorTheme.Light))]
        [OnChangedMethod(nameof(OnColorThemeChanged))]
        public ColorTheme ColorTheme { get; set; }

        private void OnColorThemeChanged()
        {
            _colorThemeResourceContainer.Clear();

            switch (ColorTheme)
            {
                case ColorTheme.System:
                    break;
                case ColorTheme.Light:
                    _colorThemeResourceContainer.Add(new ResourceDictionary { Source = GetType().Assembly.GeneratePackUri("Themes/LightTheme.xaml") });
                    break;
                case ColorTheme.Dark:
                    _colorThemeResourceContainer.Add(new ResourceDictionary { Source = GetType().Assembly.GeneratePackUri("Themes/DarkTheme.xaml") });
                    break;
            }
        }
    }
}