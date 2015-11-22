namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    [Export]
    public sealed class VsixShellViewModel : ObservableObject, IDisposable
    {
        private readonly ResourceManager _resourceManager;

        [ImportingConstructor]
        public VsixShellViewModel(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);

            _resourceManager = resourceManager;
            _resourceManager.SelectedEntitiesChanged += ResourceManager_SelectedEntitiesChanged;
        }

        void ResourceManager_SelectedEntitiesChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(() => SelectedCodeGenerators);
        }

        public ICommand CodeProviderCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<CodeGenerator>(CodeProviderClicked);
            }
        }

        public CodeGenerator SelectedCodeGenerators
        {
            get
            {
                var items = _resourceManager.SelectedEntities
                    .Select(x => x.NeutralProjectFile)
                    .OfType<DteProjectFile>()
                    .Select(pf => pf.CodeGenerator)
                    .Distinct()
                    .ToArray();

                return items.Length == 1 ? items[0] : CodeGenerator.None;
            }
        }

        private void CodeProviderClicked(CodeGenerator codeGenerator)
        {
            _resourceManager.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .ForEach(pf => pf.CodeGenerator = codeGenerator);

            OnPropertyChanged(() => SelectedCodeGenerators);
        }

        public void Dispose()
        {
            _resourceManager.SelectedEntitiesChanged -= ResourceManager_SelectedEntitiesChanged;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
        }
    }
}
