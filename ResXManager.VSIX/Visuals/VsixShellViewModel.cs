namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;

    [Export]
    public sealed class VsixShellViewModel : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private readonly DispatcherThrottle _selectedCodeGeneratorsChangedThrottle;

        [ImportingConstructor]
        public VsixShellViewModel(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);

            _selectedCodeGeneratorsChangedThrottle = new DispatcherThrottle(() => OnPropertyChanged(nameof(SelectedCodeGenerators)));
            _resourceManager = resourceManager;
            _resourceManager.SelectedEntities.CollectionChanged += (_, __) => _selectedCodeGeneratorsChangedThrottle.Tick();
            _resourceManager.Loaded += (_, __) => _selectedCodeGeneratorsChangedThrottle.Tick();
        }

        public ICommand SetCodeProviderCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<CodeGenerator>(CanSetCodeProvider, SetCodeProvider);
            }
        }

        public CodeGenerator SelectedCodeGenerators
        {
            get
            {
                var items = SelectedItemsCodeGenerators().ToArray();
                var generator = items.FirstOrDefault();

                if ((items.Length == 1) && Enum.IsDefined(typeof(CodeGenerator), generator))
                    return generator;

                return CodeGenerator.None;
            }
        }

        private IEnumerable<CodeGenerator> SelectedItemsCodeGenerators()
        {
            return _resourceManager.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .Select(pf => pf.CodeGenerator)
                .Distinct();
        }

        private bool CanSetCodeProvider(CodeGenerator obj)
        {
            return SelectedItemsCodeGenerators().All(g => g != CodeGenerator.None);
        }

        private void SetCodeProvider(CodeGenerator codeGenerator)
        {
            _resourceManager.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .ForEach(pf => pf.CodeGenerator = codeGenerator);

            _selectedCodeGeneratorsChangedThrottle.Tick();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_selectedCodeGeneratorsChangedThrottle != null);
        }
    }
}
