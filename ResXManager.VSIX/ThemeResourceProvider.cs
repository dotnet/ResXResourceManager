namespace tomenglertde.ResXManager.VSIX
{
    using System.ComponentModel.Composition;
    using System.Windows;

    using TomsToolbox.Core;
    using TomsToolbox.Wpf.Styles;

    [Export(typeof(IThemeResourceProvider))]
    internal class ThemeResourceProvider : IThemeResourceProvider
    {
        public void LoadThemeResources(ResourceDictionary resource)
        {
            resource.MergedDictionaries.Insert(0, new ResourceDictionary { Source = GetType().Assembly.GeneratePackUri("Resources/VSColorScheme.xaml") });
        }
    }
}
