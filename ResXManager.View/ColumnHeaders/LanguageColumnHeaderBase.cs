namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;

    public abstract class LanguageColumnHeaderBase : ObservableObject, ILanguageColumnHeader
    {
        private readonly CultureKey _cultureKey;
        private readonly ResourceManager _resourceManager;

        protected LanguageColumnHeaderBase(ResourceManager resourceManager, CultureKey cultureKey)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(cultureKey != null);

            _resourceManager = resourceManager;
            _cultureKey = cultureKey;
        }

        public CultureKey CultureKey => _cultureKey;

        public CultureInfo EffectiveCulture => _cultureKey.Culture ?? _resourceManager.Configuration.NeutralResourcesLanguage;

        public abstract ColumnType ColumnType
        {
            get;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_cultureKey != null);
        }
    }
}