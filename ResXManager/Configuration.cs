namespace ResXManager
{
    using System.ComponentModel.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    public class StandaloneConfiguration : Configuration
    {
        [ImportingConstructor]
        public StandaloneConfiguration([NotNull] ITracer tracer)
            : base(tracer)
        {
        }

        public override bool IsScopeSupported => false;

        public override ConfigurationScope Scope => ConfigurationScope.Global;
    }
}
