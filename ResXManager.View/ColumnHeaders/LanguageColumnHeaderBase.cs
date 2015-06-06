namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Tools;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    public abstract class LanguageColumnHeaderBase : ObservableObject, ILanguageColumnHeader
    {
        private readonly CultureKey _cultureKey;
        private readonly PropertyBinding<CultureInfo> _neutralResourcesLanguageBinding;

        protected LanguageColumnHeaderBase(ResourceManager resourceManager, CultureKey cultureKey)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(cultureKey != null);

            _cultureKey = cultureKey;
            _neutralResourcesLanguageBinding = new PropertyBinding<CultureInfo>(resourceManager, "Configuration.NeutralResourcesLanguage");
            _neutralResourcesLanguageBinding.ValueChanged += NeutralResourcesLanguage_Changed;

            NeutralCultureCountryOverrides.Default.OverrideChanged += NeutralCultureCountyOverrides_OverrideChanged;
        }

        private void NeutralResourcesLanguage_Changed(object sender, PropertyBindingValueChangedEventArgs<CultureInfo> e)
        {
            if (_cultureKey.Culture == null)
            {
                OnPropertyChanged(() => CultureKey);
                OnPropertyChanged(() => EffectiveCulture);
            }
        }

        private void NeutralCultureCountyOverrides_OverrideChanged(object sender, CultureOverrideEventArgs e)
        {
            if (e.NeutralCulture.Equals(_cultureKey.Culture))
            {
                OnPropertyChanged(() => CultureKey);
            }
        }

        public CultureKey CultureKey
        {
            get
            {
                return _cultureKey;
            }
        }

        public CultureInfo EffectiveCulture
        {
            get
            {
                return _cultureKey.Culture ?? _neutralResourcesLanguageBinding.Value ?? CultureInfo.InvariantCulture;
            }
        }

        public abstract ColumnType ColumnType
        {
            get;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_cultureKey != null);
            Contract.Invariant(_neutralResourcesLanguageBinding != null);
        }
    }
}
