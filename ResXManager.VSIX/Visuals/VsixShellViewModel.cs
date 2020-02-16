namespace ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    using Throttle;

    using ResXManager.Model;
    using ResXManager.View.Visuals;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export]
    public sealed class VsixShellViewModel : ObservableObject
    {
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;
        [NotNull]
        private readonly ShellViewModel _shellViewModel;

        [ImportingConstructor]
        public VsixShellViewModel([NotNull] ResourceManager resourceManager, [NotNull] ResourceViewModel resourceViewModel, [NotNull] ShellViewModel shellViewModel)
        {
            _resourceViewModel = resourceViewModel;
            _shellViewModel = shellViewModel;
            resourceManager.Loaded += (_, __) => SelectedCodeGeneratorsChanged();

            resourceViewModel.SelectedEntities.CollectionChanged += (_, __) => SelectedCodeGeneratorsChanged();
        }

        [NotNull]
        public ICommand SetCodeProviderCommand => new DelegateCommand<CodeGenerator>(CanSetCodeProvider, SetCodeProvider);

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

        public void SelectEntry([NotNull] ResourceTableEntry entry)
        {
            VSPackage.Instance.ShowToolWindow();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                _shellViewModel.SelectEntry(entry);
            });
        }

        [NotNull]
        private IEnumerable<CodeGenerator> SelectedItemsCodeGenerators()
        {
            return _resourceViewModel.SelectedEntities
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
            _resourceViewModel.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .ForEach(pf => pf.CodeGenerator = codeGenerator);

            SelectedCodeGeneratorsChanged();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void SelectedCodeGeneratorsChanged()
        {
            OnPropertyChanged(nameof(SelectedCodeGenerators));
        }
    }
}
