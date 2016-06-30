namespace tomenglertde.ResXManager.VSIX
{
    using System.ComponentModel.Composition;
    using System.Windows;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;

    [Export(typeof(IThemeResourceProvider))]
    internal class ThemeResourceProvider : IThemeResourceProvider
    {
        public void LoadThemeResources(ResourceDictionary resource)
        {
            resource.MergedDictionaries.Insert(0, new ResourceDictionary { Source = this.GetType().Assembly.GeneratePackUri("Resources/VSColorScheme.xaml") });
        }
    }
}
