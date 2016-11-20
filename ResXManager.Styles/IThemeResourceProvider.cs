namespace tomenglertde.ResXManager.Styles
{
    using System.Diagnostics.Contracts;
    using System.Windows;

    using JetBrains.Annotations;

    [ContractClass(typeof(ThemeResourceProviderContract))]
    public interface IThemeResourceProvider
    {
        void LoadThemeResources([NotNull] ResourceDictionary resource);
    }

    [ContractClassFor(typeof(IThemeResourceProvider))]
    internal abstract class ThemeResourceProviderContract : IThemeResourceProvider
    {
        public void LoadThemeResources(ResourceDictionary resource)
        {
            Contract.Requires(resource != null);
            throw new System.NotImplementedException();
        }
    }
}
