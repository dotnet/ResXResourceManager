namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public enum DuplicateKeyHandling
    {
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Rename)]
        Rename,
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Fail)]
        Fail
    }

    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Works fine with this")]
    public abstract class Configuration : ConfigurationBase
    {
        [NotNull]
        private readonly DispatcherThrottle _codeReferencesChangeThrottle;
        private CodeReferenceConfiguration _codeReferences;

        protected Configuration([NotNull] ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(tracer != null);

            _codeReferencesChangeThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, PersistCodeReferences);
        }

        [NotNull]
        public CodeReferenceConfiguration CodeReferences
        {
            get
            {
                Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

                return _codeReferences ?? LoadCodeReferenceConfiguration();
            }
        }

        public bool SaveFilesImmediatelyUponChange
        {
            get
            {
                return GetValue(true);
            }
            set
            {
                SetValue(value);
            }
        }

        public bool SortFileContentOnSave
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        [NotNull]
        public CultureInfo NeutralResourcesLanguage
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                return GetValue(default(CultureInfo)) ?? new CultureInfo("en-US");
            }
            set
            {
                SetValue(value);
            }
        }

        public StringComparison ResXSortingComparison
        {
            get
            {
                return GetValue(StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                SetValue(value);
            }
        }

        public StringComparison? EffectiveResXSortingComparison => SortFileContentOnSave ? ResXSortingComparison : (StringComparison?) null;

        public bool ConfirmAddLanguageFile
        {
            get
            {
                return GetValue(true);
            }
            set
            {
                SetValue(value);
            }
        }

        public bool AutoCreateNewLanguageFiles
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        public bool PrefixTranslations
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        public string TranslationPrefix
        {
            get
            {
                return GetValue("#TODO#_");
            }
            set
            {
                SetValue(value);
            }
        }

        public string EffectiveTranslationPrefix => PrefixTranslations ? TranslationPrefix : string.Empty;

        public ExcelExportMode ExcelExportMode
        {
            get
            {
                return GetValue(default(ExcelExportMode));
            }
            set
            {
                SetValue(value);
            }
        }

        public DuplicateKeyHandling DuplicateKeyHandling
        {
            get
            {
                return GetValue(default(DuplicateKeyHandling));
            }
            set
            {
                SetValue(value);
            }
        }

        public bool ShowPerformanceTraces
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        public void Reload()
        {
            OnReload();
        }

        protected virtual void OnReload()
        {
            _codeReferences = null;

            // ReSharper disable once PossibleNullReferenceException
            GetType().GetProperties().ForEach(p => OnPropertyChanged(p.Name));
        }

        private void PersistCodeReferences()
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            SetValue(CodeReferences, nameof(CodeReferences));
        }

        [NotNull]
        private CodeReferenceConfiguration LoadCodeReferenceConfiguration()
        {
            Contract.Ensures(Contract.Result<CodeReferenceConfiguration>() != null);

            // ReSharper disable once ExplicitCallerInfoArgument
            _codeReferences = GetValue(default(CodeReferenceConfiguration), nameof(CodeReferences)) ?? CodeReferenceConfiguration.Default;
            _codeReferences.ItemPropertyChanged += (_, __) => _codeReferencesChangeThrottle.Tick();

            return _codeReferences;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_codeReferencesChangeThrottle != null);
        }
    }
}
