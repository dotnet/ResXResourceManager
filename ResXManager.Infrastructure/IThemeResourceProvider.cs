namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Diagnostics.Contracts;
    using System.Windows;

    [ContractClass(typeof(ThemeResourceProviderContract))]
    public interface IThemeResourceProvider
    {
        void LoadThemeResources(ResourceDictionary resource);
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
