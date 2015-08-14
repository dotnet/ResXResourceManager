namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;

    using tomenglertde.ResXManager.Model;

    [Export(typeof(Configuration))]
    class StandaloneConfiguration : Configuration
    {
        private StandaloneConfiguration()
        {
        }

        public override bool IsScopeSupported
        {
            get
            {
                return false;
            }
        }

        public override ConfigurationScope Scope
        {
            get
            {
                return ConfigurationScope.Global;
            }
        }
    }
}
