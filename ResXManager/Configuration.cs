namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    [Export(typeof(Configuration))]
    class StandaloneConfiguration : Configuration
    {
        [ImportingConstructor]
        private StandaloneConfiguration(ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(tracer != null);
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
