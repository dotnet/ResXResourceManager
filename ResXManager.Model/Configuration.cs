namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Works fine with this")]
    public class Configuration : ConfigurationBase
    {
        private CodeReferenceConfiguration _codeReferences;

        public CodeReferenceConfiguration CodeReferences
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                return _codeReferences ?? (_codeReferences = GetValue(() => CodeReferences) ?? CodeReferenceConfiguration.Default);
            }
        }

        public bool SortFileContentOnSave
        {
            get
            {
                return GetValue(() => SortFileContentOnSave);
            }
            set
            {
                SetValue(value, () => SortFileContentOnSave);
            }
        }

        public CultureInfo NeutralResourcesLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return GetValue(() => NeutralResourcesLanguage) ?? new CultureInfo("en-US");
            }
            set
            {
                SetValue(value, () => NeutralResourcesLanguage);
            }
        }

        public void PersistCodeReferences()
        {
            SetValue(CodeReferences, () => CodeReferences);
        }
    }
}
