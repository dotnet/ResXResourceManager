namespace ResXManager
{
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    [Shared]
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
