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
    }
}
